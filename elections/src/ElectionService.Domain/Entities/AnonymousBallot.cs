using ElectionService.Domain.Enums;

namespace ElectionService.Domain.Entities;

public sealed class AnonymousBallot
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public SelectionType SelectionType { get; set; }
    public Guid SelectionId { get; set; }
    public string? TerritoryCode { get; set; }
}