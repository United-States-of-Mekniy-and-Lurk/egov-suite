using ElectionService.Domain.Enums;

namespace ElectionService.Domain.Entities;

public sealed class ElectionResult
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public SelectionType SelectionType { get; set; }
    public Guid SelectionId { get; set; }
    public string SelectionLabel { get; set; } = string.Empty;
    public string? TerritoryCode { get; set; }
    public int VoteCount { get; set; }
    public DateTime FinalizedAt { get; set; }
}