namespace ElectionService.Domain.Entities;

public sealed class CertificationDecision
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Guid CertifierPersonId { get; set; }
    public bool IsApproved { get; set; }
    public string? Reason { get; set; }
    public DateTime DecidedAt { get; set; }
}
