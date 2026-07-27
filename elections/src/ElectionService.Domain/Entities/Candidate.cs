namespace ElectionService.Domain.Entities;

public sealed class Candidate
{
    public Guid Id { get; set; }
    public Guid PartyListId { get; set; }
    public PartyList PartyList { get; set; } = null!;
    public Guid? PersonId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Position { get; set; }
    public DateTime? WithdrawnAt { get; set; }
}