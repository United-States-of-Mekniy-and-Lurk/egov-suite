namespace OrganizationRegistry.Domain.Entities;

public sealed class LegalFormDefinition
{
    public string Code { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelCs { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionCs { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}