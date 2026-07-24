using System.Text.Json;

namespace CitizenService.Web.Models;

public class PersonViewModel
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = string.Empty;
    public string PreferredUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CitizenViewModel
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string CitizenNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? GrantedAt { get; set; }
    public string? ImportSource { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CitizenListItemViewModel
{
    public required CitizenViewModel Citizen { get; init; }
    public PersonViewModel? Person { get; init; }
}

public class ApplicationViewModel
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public int FormVersion { get; set; }
    public JsonElement? FormAnswers { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApplicationFormViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplicationFormDraftViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}

public class AdminSidebarViewModel
{
    public string ActiveSection { get; set; } = string.Empty;
    public List<RegistryFieldDefinitionViewModel> Fields { get; set; } = [];
}

public class RegistryFieldDefinitionViewModel
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = [];
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool UserEditable { get; set; }
    public int SortOrder { get; set; }
    public string OptionSourceType { get; set; } = "None";
    public List<RegistryFieldOptionViewModel>? StaticOptions { get; set; }
    public string? OptionSourceService { get; set; }
    public string? OptionSourcePath { get; set; }
    public bool IsActive { get; set; }

    public string GetLabel(string culture)
    {
        if (Labels.TryGetValue(culture, out var localized)) return localized;
        if (Labels.TryGetValue("en", out var english)) return english;
        return Key;
    }
}

public class RegistryFieldOptionViewModel
{
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = [];

    public string GetLabel(string culture)
    {
        if (Labels.TryGetValue(culture, out var localized)) return localized;
        if (Labels.TryGetValue("en", out var english)) return english;
        return Value;
    }
}

public class CitizenRegistryFieldViewModel
{
    public RegistryFieldDefinitionViewModel Definition { get; set; } = new();
    public string? Value { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? SourceApplicationId { get; set; }
}

public class CitizenRegistryFieldHistoryViewModel
{
    public RegistryFieldDefinitionViewModel Definition { get; set; } = new();
    public string Value { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid RecordedByPersonId { get; set; }
    public Guid? SourceApplicationId { get; set; }
    public Guid? SourceCorrectionRequestId { get; set; }
}

public class FieldCorrectionRequestViewModel
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public Guid PersonId { get; set; }
    public RegistryFieldDefinitionViewModel Definition { get; set; } = new();
    public string? CurrentValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string RequestReason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid RequestedByPersonId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public Guid? ReviewedByPersonId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
}
