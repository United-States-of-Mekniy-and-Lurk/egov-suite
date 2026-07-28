namespace Egov.Platform.Feeds;

/// <summary>
/// Channel-level metadata for an RSS 2.0 feed.
/// </summary>
public sealed record RssFeedChannel
{
    public required string Title { get; init; }
    public required string Link { get; init; }
    public required string Description { get; init; }
    public string? Language { get; init; }
    public string? Copyright { get; init; }
    public string? ManagingEditor { get; init; }
    public string? WebMaster { get; init; }
    public int? TimeToLiveMinutes { get; init; }
}
