using System.Net.Http.Json;
using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public class RegistryFieldsModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<RegistryFieldsModel> _logger;

    public List<RegistryFieldDefinitionViewModel> Definitions { get; set; } = [];

    [BindProperty] public string Key { get; set; } = string.Empty;
    [BindProperty] public string LabelEn { get; set; } = string.Empty;
    [BindProperty] public string LabelCs { get; set; } = string.Empty;
    [BindProperty] public string FieldType { get; set; } = "Text";
    [BindProperty] public bool IsRequired { get; set; }
    [BindProperty] public bool UserEditable { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public string OptionSourceType { get; set; } = "None";
    [BindProperty] public string? StaticOptions { get; set; }
    [BindProperty] public string? OptionSourceService { get; set; }
    [BindProperty] public string? OptionSourcePath { get; set; }
    [BindProperty] public Guid? EditingId { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public RegistryFieldsModel(
        IHttpClientFactory httpClientFactory,
        IStringLocalizer localizer,
        ILogger<RegistryFieldsModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid? edit, CancellationToken ct)
    {
        await LoadDefinitionsAsync(ct);
        if (edit is null) return Page();

        var definition = Definitions.SingleOrDefault(field => field.Id == edit);
        if (definition is null) return NotFound();

        EditingId = definition.Id;
        Key = definition.Key;
        LabelEn = definition.Labels.GetValueOrDefault("en", definition.Key);
        LabelCs = definition.Labels.GetValueOrDefault("cs", string.Empty);
        FieldType = definition.FieldType;
        IsRequired = definition.IsRequired;
        UserEditable = definition.UserEditable;
        SortOrder = definition.SortOrder;
        OptionSourceType = definition.OptionSourceType;
        StaticOptions = FormatStaticOptions(definition.StaticOptions);
        OptionSourceService = definition.OptionSourceService;
        OptionSourcePath = definition.OptionSourcePath;
        IsActive = definition.IsActive;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var labels = new Dictionary<string, string> { ["en"] = LabelEn };
        if (!string.IsNullOrWhiteSpace(LabelCs)) labels["cs"] = LabelCs;

        var options = ParseStaticOptions(StaticOptions);
        var body = new
        {
            key = Key,
            labels,
            fieldType = FieldType,
            isRequired = IsRequired,
            userEditable = UserEditable,
            sortOrder = SortOrder,
            optionSourceType = OptionSourceType,
            staticOptions = options,
            optionSourceService = OptionSourceService,
            optionSourcePath = OptionSourcePath,
            isActive = IsActive
        };

        var client = _httpClientFactory.CreateClient("CitizenApi");
        HttpResponseMessage response;
        try
        {
            response = EditingId is { } id
                ? await client.PutAsJsonAsync($"/registry-fields/{id}", body, JsonOptions, ct)
                : await client.PostAsJsonAsync("/registry-fields", body, JsonOptions, ct);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Registry field save request failed before receiving a response");
            IsError = true;
            Message = _localizer["admin.registry_fields.request_failed"];
            await LoadDefinitionsAsync(ct);
            return Page();
        }

        IsError = !response.IsSuccessStatusCode;
        Message = response.IsSuccessStatusCode
            ? _localizer[EditingId is null
                ? "admin.registry_fields.created"
                : "admin.registry_fields.updated"]
            : await ReadErrorAsync(response, _localizer, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Registry field save returned {StatusCode}: {Error}",
                (int)response.StatusCode,
                Message);
        }

        await LoadDefinitionsAsync(ct);
        return Page();
    }

    private async Task LoadDefinitionsAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/registry-fields?includeInactive=true", ct);
        if (!response.IsSuccessStatusCode) return;
        Definitions = await response.Content.ReadFromJsonAsync<List<RegistryFieldDefinitionViewModel>>(
            JsonOptions, ct) ?? [];
    }

    private static List<object>? ParseStaticOptions(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        return input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => (object)new
            {
                value = parts[0],
                labels = new Dictionary<string, string>
                {
                    ["en"] = parts[1],
                    ["cs"] = parts.Length >= 3 ? parts[2] : parts[1]
                }
            })
            .ToList();
    }

    private static string? FormatStaticOptions(List<RegistryFieldOptionViewModel>? options)
    {
        if (options is null || options.Count == 0) return null;

        return string.Join('\n', options.Select(option => string.Join(" | ",
            option.Value,
            option.Labels.GetValueOrDefault("en", option.Value),
            option.Labels.GetValueOrDefault("cs", option.Labels.GetValueOrDefault("en", option.Value)))));
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        IStringLocalizer localizer,
        CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
        {
            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden => localizer["admin.registry_fields.forbidden"],
                System.Net.HttpStatusCode.NotFound => localizer["admin.registry_fields.endpoint_missing"],
                _ => localizer["admin.registry_fields.http_error", (int)response.StatusCode]
            };
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error))
                return error.GetString() ?? content;
            if (document.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? content;
            return content;
        }
        catch (JsonException)
        {
            return content;
        }
    }
}
