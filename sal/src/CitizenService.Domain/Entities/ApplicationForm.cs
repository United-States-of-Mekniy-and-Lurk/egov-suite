namespace CitizenService.Domain.Entities;

public class ApplicationForm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
