using CitizenService.Domain.Enums;

namespace CitizenService.Application.Models;

public record RegistryFieldDefinitionInput(
    string Key,
    Dictionary<string, string> Labels,
    RegistryFieldType FieldType,
    bool IsRequired,
    bool UserEditable,
    int SortOrder,
    FieldOptionSourceType OptionSourceType,
    List<RegistryFieldOptionInput>? StaticOptions,
    string? OptionSourceService,
    string? OptionSourcePath,
    bool IsActive = true);

public record RegistryFieldOptionInput(
    string Value,
    Dictionary<string, string> Labels);

public record RegistryFieldDefinitionDto(
    Guid Id,
    string Key,
    Dictionary<string, string> Labels,
    RegistryFieldType FieldType,
    bool IsRequired,
    bool UserEditable,
    int SortOrder,
    FieldOptionSourceType OptionSourceType,
    List<RegistryFieldOptionInput>? StaticOptions,
    string? OptionSourceService,
    string? OptionSourcePath,
    bool IsActive);

public record CitizenRegistryFieldDto(
    RegistryFieldDefinitionDto Definition,
    string? Value,
    DateTime? UpdatedAt,
    Guid? SourceApplicationId);

public record CitizenRegistryFieldHistoryDto(
    RegistryFieldDefinitionDto Definition,
    string Value,
    DateTime ValidFrom,
    DateTime? ValidTo,
    DateTime RecordedAt,
    Guid RecordedByPersonId,
    Guid? SourceApplicationId,
    Guid? SourceCorrectionRequestId);
