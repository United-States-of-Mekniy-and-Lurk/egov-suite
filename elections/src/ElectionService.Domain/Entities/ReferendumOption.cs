namespace ElectionService.Domain.Entities;

public sealed class ReferendumOption
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Election Election { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}