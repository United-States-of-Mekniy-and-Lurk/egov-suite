namespace CitizenService.Domain.Entities;

public class CitizenFieldValue
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public Guid? SourceApplicationId { get; set; }
    public Guid UpdatedByPersonId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}