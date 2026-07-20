namespace CitizenService.Domain.Entities;

public class ApplicationFormDraft
{
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedByPersonId { get; set; }
}