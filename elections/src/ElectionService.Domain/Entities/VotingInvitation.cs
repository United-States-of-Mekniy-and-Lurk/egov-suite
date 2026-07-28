namespace ElectionService.Domain.Entities;

public sealed class VotingInvitation
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Label { get; set; }
    public Guid? PersonId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public DateOnly? UsedOn { get; set; }
    public DateTime? RevokedAt { get; set; }
}