namespace OrganizationRegistry.Domain.Entities;

public sealed class ClassificationDefinition
{
    public Guid Id { get; set; }
    public string Scheme { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelCs { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class OrganizationClassification
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid DefinitionId { get; set; }
    public ClassificationDefinition Definition { get; set; } = null!;
    public DateTime AssignedAt { get; set; }
    public Guid AssignedByPersonId { get; set; }
}