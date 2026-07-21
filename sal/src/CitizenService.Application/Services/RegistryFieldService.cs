using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CitizenService.Application.Interfaces;
using CitizenService.Application.Models;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Services;

public partial class RegistryFieldService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRegistryFieldRepository _registryRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IFormRepository _formRepository;
    private readonly ICurrentActor _currentActor;

    public RegistryFieldService(
        IRegistryFieldRepository registryRepository,
        ICitizenRepository citizenRepository,
        IApplicationRepository applicationRepository,
        IFormRepository formRepository,
        ICurrentActor currentActor)
    {
        _registryRepository = registryRepository;
        _citizenRepository = citizenRepository;
        _applicationRepository = applicationRepository;
        _formRepository = formRepository;
        _currentActor = currentActor;
    }

    public async Task<IReadOnlyList<RegistryFieldDefinitionDto>> ListDefinitionsAsync(
        bool includeInactive, CancellationToken ct)
    {
        var definitions = await _registryRepository.ListDefinitionsAsync(includeInactive, ct);
        return definitions.Select(ToDto).ToList();
    }

    public async Task<RegistryFieldDefinitionDto> CreateDefinitionAsync(
        RegistryFieldDefinitionInput input, CancellationToken ct)
    {
        ValidateDefinition(input);
        if (await _registryRepository.GetDefinitionByKeyAsync(input.Key, ct) != null)
            throw new ArgumentException($"Registry field '{input.Key}' already exists.");

        var now = DateTime.UtcNow;
        var definition = new RegistryFieldDefinition
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyInput(definition, input);
        await _registryRepository.AddDefinitionAsync(definition, ct);
        return ToDto(definition);
    }

    public async Task<RegistryFieldDefinitionDto> UpdateDefinitionAsync(
        Guid id, RegistryFieldDefinitionInput input, CancellationToken ct)
    {
        ValidateDefinition(input);
        var definition = await _registryRepository.GetDefinitionByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Registry field {id} was not found.");
        var duplicate = await _registryRepository.GetDefinitionByKeyAsync(input.Key, ct);
        if (duplicate != null && duplicate.Id != id)
            throw new ArgumentException($"Registry field '{input.Key}' already exists.");

        var proposedDefinition = new RegistryFieldDefinition();
        ApplyInput(proposedDefinition, input);
        var existingValues = await _registryRepository.ListValuesByDefinitionAsync(id, ct);
        foreach (var existingValue in existingValues)
            NormalizeValue(proposedDefinition, existingValue.Value);

        ApplyInput(definition, input);
        definition.UpdatedAt = DateTime.UtcNow;
        await _registryRepository.UpdateDefinitionAsync(definition, ct);
        return ToDto(definition);
    }

    public async Task<IReadOnlyList<CitizenRegistryFieldDto>> GetCitizenFieldsAsync(
        Guid personId, CancellationToken ct)
    {
        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {personId}.");
        var definitions = await _registryRepository.ListDefinitionsAsync(includeInactive: false, ct);
        var values = await _registryRepository.ListValuesAsync(citizen.Id, ct);
        var valuesByDefinition = values.ToDictionary(value => value.FieldDefinitionId);

        return definitions.Select(definition =>
        {
            valuesByDefinition.TryGetValue(definition.Id, out var value);
            return new CitizenRegistryFieldDto(
                ToDto(definition),
                value?.Value,
                value?.UpdatedAt,
                value?.SourceApplicationId);
        }).ToList();
    }

    public async Task<CitizenRegistryFieldDto> SetCitizenFieldAsync(
        Guid personId,
        string fieldKey,
        string? value,
        Guid? sourceApplicationId,
        Guid? sourceCorrectionRequestId,
        CancellationToken ct)
    {
        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {personId}.");
        var definition = await _registryRepository.GetDefinitionByKeyAsync(fieldKey, ct)
            ?? throw new KeyNotFoundException($"Registry field '{fieldKey}' was not found.");
        if (!definition.IsActive)
            throw new ArgumentException($"Registry field '{fieldKey}' is inactive.");
        if (sourceApplicationId.HasValue)
        {
            var sourceApplication = await _applicationRepository.GetByIdAsync(sourceApplicationId.Value, ct)
                ?? throw new ArgumentException("Source application was not found.");
            if (sourceApplication.PersonId != personId)
                throw new ArgumentException("Source application belongs to a different person.");
        }

        var normalizedValue = NormalizeValue(definition, value);
        var now = DateTime.UtcNow;
        var currentValue = await _registryRepository.GetValueAsync(citizen.Id, definition.Id, ct);
        if (currentValue?.Value == normalizedValue)
        {
            return new CitizenRegistryFieldDto(
                ToDto(definition), currentValue.Value, currentValue.UpdatedAt, currentValue.SourceApplicationId);
        }

        var fieldValue = new CitizenFieldValue
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            FieldDefinitionId = definition.Id,
            Value = normalizedValue,
            SourceApplicationId = sourceApplicationId,
            SourceCorrectionRequestId = sourceCorrectionRequestId,
            UpdatedByPersonId = _currentActor.PersonId,
            ValidFrom = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _registryRepository.ReplaceCurrentValueAsync(currentValue, fieldValue, ct);

        return new CitizenRegistryFieldDto(
            ToDto(definition), fieldValue.Value, fieldValue.UpdatedAt, fieldValue.SourceApplicationId);
    }

    public async Task<IReadOnlyList<CitizenRegistryFieldHistoryDto>> GetCitizenFieldHistoryAsync(
        Guid personId,
        CancellationToken ct)
    {
        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {personId}.");
        var definitions = (await _registryRepository.ListDefinitionsAsync(includeInactive: true, ct))
            .ToDictionary(definition => definition.Id);
        var values = await _registryRepository.ListValueHistoryAsync(citizen.Id, ct);
        return values
            .Where(value => definitions.ContainsKey(value.FieldDefinitionId))
            .Select(value => new CitizenRegistryFieldHistoryDto(
                ToDto(definitions[value.FieldDefinitionId]),
                value.Value,
                value.ValidFrom,
                value.ValidTo,
                value.CreatedAt,
                value.UpdatedByPersonId,
                value.SourceApplicationId,
                value.SourceCorrectionRequestId))
            .ToList();
    }

    public async Task ApplyApplicationAnswersAsync(
        CitizenshipApplication application, CancellationToken ct)
    {
        if (application.FormAnswers == null ||
            application.FormAnswers.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("The application has no form answers.");
        }

        var form = await _formRepository.GetFormAsync(
            application.FormName, application.FormVersion, ct)
            ?? throw new ArgumentException("The submitted application form version was not found.");
        using var formDefinition = JsonDocument.Parse(form.DefinitionJson);
        var formFieldKeys = ReadFormFieldKeys(formDefinition.RootElement);
        var definitions = await _registryRepository.ListDefinitionsAsync(includeInactive: false, ct);
        var citizen = await _citizenRepository.GetByPersonIdAsync(application.PersonId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {application.PersonId}.");

        foreach (var definition in definitions)
        {
            if (application.FormAnswers.RootElement.TryGetProperty(definition.Key, out var answer) &&
                answer.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                await SetCitizenFieldAsync(
                    application.PersonId,
                    definition.Key,
                    JsonValueToString(answer),
                    application.Id,
                    null,
                    ct);
                continue;
            }

            if (definition.IsRequired && formFieldKeys.Contains(definition.Key))
            {
                var existing = await _registryRepository.GetValueAsync(citizen.Id, definition.Id, ct);
                if (existing == null || string.IsNullOrWhiteSpace(existing.Value))
                    throw new ArgumentException($"Required registry field '{definition.Key}' has no submitted answer.");
            }
        }
    }

    public string NormalizeValue(RegistryFieldDefinition definition, string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            if (definition.IsRequired)
                throw new ArgumentException($"Registry field '{definition.Key}' is required.");
            return string.Empty;
        }

        return definition.FieldType switch
        {
            RegistryFieldType.Text or RegistryFieldType.MultilineText => trimmed,
            RegistryFieldType.Date => NormalizeDate(definition, trimmed),
            RegistryFieldType.Integer => long.TryParse(
                trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)
                    ? integer.ToString(CultureInfo.InvariantCulture)
                    : throw InvalidValue(definition, "an integer"),
            RegistryFieldType.Decimal => decimal.TryParse(
                trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                    ? number.ToString("G29", CultureInfo.InvariantCulture)
                    : throw InvalidValue(definition, "a decimal number"),
            RegistryFieldType.Boolean => bool.TryParse(trimmed, out var boolean)
                ? boolean.ToString().ToLowerInvariant()
                : throw InvalidValue(definition, "true or false"),
            RegistryFieldType.Select => NormalizeSelectValue(definition, trimmed),
            _ => throw new ArgumentOutOfRangeException(nameof(definition.FieldType))
        };
    }

    private static string NormalizeSelectValue(RegistryFieldDefinition definition, string value)
    {
        if (definition.OptionSourceType != FieldOptionSourceType.Static)
            return value;

        var options = DeserializeOptions(definition.StaticOptionsJson) ?? [];
        if (options.All(option => option.Value != value))
            throw InvalidValue(definition, "one of the configured options");
        return value;
    }

    private static string NormalizeDate(RegistryFieldDefinition definition, string value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return DateOnly.FromDateTime(timestamp.Date).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        throw InvalidValue(definition, "an ISO date");
    }

    private static HashSet<string> ReadFormFieldKeys(JsonElement definition)
    {
        if (definition.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
        {
            return fields.EnumerateArray()
                .Where(field => field.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                .Select(field => field.GetProperty("name").GetString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToHashSet(StringComparer.Ordinal);
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (definition.TryGetProperty("components", out var components) &&
            components.ValueKind == JsonValueKind.Array)
        {
            ReadFormioRegistryFieldKeys(components, keys);
        }

        return keys;
    }

    private static void ReadFormioRegistryFieldKeys(JsonElement components, HashSet<string> keys)
    {
        foreach (var component in components.EnumerateArray())
        {
            if (component.TryGetProperty("key", out var key) &&
                key.ValueKind == JsonValueKind.String &&
                component.TryGetProperty("properties", out var properties) &&
                properties.ValueKind == JsonValueKind.Object &&
                properties.TryGetProperty("persistence", out var persistence) &&
                persistence.ValueKind == JsonValueKind.String &&
                persistence.GetString() == "registry")
            {
                var value = key.GetString();
                if (!string.IsNullOrWhiteSpace(value)) keys.Add(value);
            }

            if (component.TryGetProperty("components", out var nested) &&
                nested.ValueKind == JsonValueKind.Array)
            {
                ReadFormioRegistryFieldKeys(nested, keys);
            }

            if (component.TryGetProperty("columns", out var columns) &&
                columns.ValueKind == JsonValueKind.Array)
            {
                foreach (var column in columns.EnumerateArray())
                {
                    if (column.TryGetProperty("components", out var columnComponents) &&
                        columnComponents.ValueKind == JsonValueKind.Array)
                    {
                        ReadFormioRegistryFieldKeys(columnComponents, keys);
                    }
                }
            }
        }
    }

    private static string? JsonValueToString(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            _ => throw new ArgumentException("Registry field answers must be scalar values.")
        };

    private static void ValidateDefinition(RegistryFieldDefinitionInput input)
    {
        if (!FieldKeyPattern().IsMatch(input.Key))
            throw new ArgumentException("Field key must start with a lowercase letter and contain only lowercase letters, digits, and underscores.");
        if (input.Labels == null ||
            !input.Labels.TryGetValue("en", out var englishLabel) ||
            string.IsNullOrWhiteSpace(englishLabel))
            throw new ArgumentException("An English field label is required.");
        if (input.SortOrder < 0)
            throw new ArgumentException("Sort order cannot be negative.");

        if (input.FieldType != RegistryFieldType.Select && input.OptionSourceType != FieldOptionSourceType.None)
            throw new ArgumentException("Only select fields may define an option source.");
        if (input.FieldType == RegistryFieldType.Select && input.OptionSourceType == FieldOptionSourceType.None)
            throw new ArgumentException("Select fields must define an option source.");
        if (input.OptionSourceType == FieldOptionSourceType.Static && (input.StaticOptions == null || input.StaticOptions.Count == 0))
            throw new ArgumentException("Static select fields require at least one option.");
        if (input.OptionSourceType == FieldOptionSourceType.Static && input.StaticOptions != null)
        {
            if (input.StaticOptions.Any(option => string.IsNullOrWhiteSpace(option.Value)))
                throw new ArgumentException("Static options require a value.");
            if (input.StaticOptions.Select(option => option.Value).Distinct().Count() != input.StaticOptions.Count)
                throw new ArgumentException("Static option values must be unique.");
            if (input.StaticOptions.Any(option => option.Labels == null ||
                !option.Labels.TryGetValue("en", out var label) || string.IsNullOrWhiteSpace(label)))
            {
                throw new ArgumentException("Every static option requires an English label.");
            }
        }
        if (input.OptionSourceType == FieldOptionSourceType.RemoteService)
        {
            if (string.IsNullOrWhiteSpace(input.OptionSourceService))
                throw new ArgumentException("Remote select fields require a service name.");
            if (string.IsNullOrWhiteSpace(input.OptionSourcePath) ||
                !input.OptionSourcePath.StartsWith('/') ||
                input.OptionSourcePath.StartsWith("//") ||
                input.OptionSourcePath.Contains(".."))
            {
                throw new ArgumentException("Remote option path must be a safe relative service path.");
            }
        }
    }

    private static void ApplyInput(RegistryFieldDefinition definition, RegistryFieldDefinitionInput input)
    {
        definition.Key = input.Key;
        definition.LabelsJson = JsonSerializer.Serialize(input.Labels, JsonOptions);
        definition.FieldType = input.FieldType;
        definition.IsRequired = input.IsRequired;
        definition.UserEditable = input.UserEditable;
        definition.SortOrder = input.SortOrder;
        definition.OptionSourceType = input.OptionSourceType;
        definition.StaticOptionsJson = input.OptionSourceType == FieldOptionSourceType.Static
            ? JsonSerializer.Serialize(input.StaticOptions, JsonOptions)
            : null;
        definition.OptionSourceService = input.OptionSourceType == FieldOptionSourceType.RemoteService
            ? input.OptionSourceService
            : null;
        definition.OptionSourcePath = input.OptionSourceType == FieldOptionSourceType.RemoteService
            ? input.OptionSourcePath
            : null;
        definition.IsActive = input.IsActive;
    }

    private static RegistryFieldDefinitionDto ToDto(RegistryFieldDefinition definition)
        => new(
            definition.Id,
            definition.Key,
            JsonSerializer.Deserialize<Dictionary<string, string>>(definition.LabelsJson, JsonOptions) ?? [],
            definition.FieldType,
            definition.IsRequired,
            definition.UserEditable,
            definition.SortOrder,
            definition.OptionSourceType,
            DeserializeOptions(definition.StaticOptionsJson),
            definition.OptionSourceService,
            definition.OptionSourcePath,
            definition.IsActive);

    private static List<RegistryFieldOptionInput>? DeserializeOptions(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<List<RegistryFieldOptionInput>>(json, JsonOptions);

    private static ArgumentException InvalidValue(RegistryFieldDefinition definition, string expected)
        => new($"Value for registry field '{definition.Key}' must be {expected}.");

    [GeneratedRegex("^[a-z][a-z0-9_]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyPattern();
}
