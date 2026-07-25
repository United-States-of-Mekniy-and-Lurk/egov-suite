using ElectionService.Domain.Enums;

namespace ElectionService.Domain.Entities;

public sealed class ElectionTransition
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public ElectionStatus FromStatus { get; set; }
    public ElectionStatus ToStatus { get; set; }
    public Guid ChangedByPersonId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}