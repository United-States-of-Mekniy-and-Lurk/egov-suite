namespace Egov.Platform.Feeds;

/// <summary>
/// Represents a single item in an RSS 2.0 feed.
/// </summary>
public sealed record RssFeedItem
{
    public required string Title { get; init; }
    public required string Link { get; init; }
    public string? Description { get; init; }
    public DateTime? PublishedAt { get; init; }
    public string? Guid { get; init; }
    public bool GuidIsPermaLink { get; init; }
    public string? Author { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
}
