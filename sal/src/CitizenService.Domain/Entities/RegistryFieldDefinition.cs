using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Entities;

public class RegistryFieldDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string LabelsJson { get; set; } = "{}";
    public RegistryFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool UserEditable { get; set; }
    public int SortOrder { get; set; }
    public FieldOptionSourceType OptionSourceType { get; set; }
    public string? StaticOptionsJson { get; set; }
    public string? OptionSourceService { get; set; }
    public string? OptionSourcePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}