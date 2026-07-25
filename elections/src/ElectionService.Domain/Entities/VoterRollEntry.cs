namespace ElectionService.Domain.Entities;

public sealed class VoterRollEntry
{
    public Guid ElectionId { get; set; }
    public Guid PersonId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid AddedByPersonId { get; set; }
}