using ElectionService.Domain.Enums;

namespace ElectionService.Domain.Entities;

public sealed class ParticipationRecord
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public ParticipationChannel Channel { get; set; }
    public string CredentialHash { get; set; } = string.Empty;
    public DateOnly RecordedOn { get; set; }
}