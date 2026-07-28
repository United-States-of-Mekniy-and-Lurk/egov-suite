namespace Egov.Platform.Feeds;

/// <summary>
/// Pluggable feed provider that supplies channel metadata and items from domain entities.
/// Each registered IFeedProvider is exposed as its own RSS endpoint.
/// </summary>
public interface IFeedProvider
{
    /// <summary>Unique feed identifier used in the URL path, e.g. "new-organizations".</summary>
    string FeedId { get; }

    /// <summary>Channel metadata for this feed.</summary>
    RssFeedChannel Channel { get; }

    /// <summary>Retrieves the most recent items for the feed.</summary>
    Task<IReadOnlyList<RssFeedItem>> GetItemsAsync(int maxItems, CancellationToken ct);
}

/// <summary>
/// Typed feed provider that maps domain entities of type <typeparamref name="T"/> to RSS items.
/// </summary>
public interface IFeedProvider<T> : IFeedProvider
{
    /// <summary>Retrieves source entities for the feed.</summary>
    Task<IReadOnlyList<T>> GetSourceEntitiesAsync(int maxItems, CancellationToken ct);

    /// <summary>Maps a source entity to an RSS feed item.</summary>
    RssFeedItem MapToItem(T entity);
}

/// <summary>
/// Base class that implements <see cref="IFeedProvider{T}"/> with automatic mapping delegation.
/// Subclasses only need to implement <see cref="GetSourceEntitiesAsync"/> and <see cref="MapToItem"/>.
/// </summary>
public abstract class FeedProvider<T> : IFeedProvider<T>
{
    public abstract string FeedId { get; }
    public abstract RssFeedChannel Channel { get; }
    public abstract Task<IReadOnlyList<T>> GetSourceEntitiesAsync(int maxItems, CancellationToken ct);
    public abstract RssFeedItem MapToItem(T entity);

    public async Task<IReadOnlyList<RssFeedItem>> GetItemsAsync(int maxItems, CancellationToken ct)
    {
        var entities = await GetSourceEntitiesAsync(maxItems, ct);
        return entities.Select(MapToItem).ToList();
    }
}
