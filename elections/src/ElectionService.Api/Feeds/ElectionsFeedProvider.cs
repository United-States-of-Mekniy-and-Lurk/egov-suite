using Egov.Platform.Feeds;
using ElectionService.Application.Abstractions;
using ElectionService.Domain.Entities;

namespace ElectionService.Api.Feeds;

public sealed class ElectionsFeedProvider(IElectionStore store) : FeedProvider<Election>
{
    public override string FeedId => "elections";

    public override RssFeedChannel Channel => new()
    {
        Title = "Elections — Election Service",
        Link = "/feeds/elections",
        Description = "Published and upcoming elections.",
        Language = "en",
        TimeToLiveMinutes = 15
    };

    public override async Task<IReadOnlyList<Election>> GetSourceEntitiesAsync(int maxItems, CancellationToken ct)
    {
        var all = await store.ListPublicAsync(ct);
        return all.Take(maxItems).ToList();
    }

    public override RssFeedItem MapToItem(Election entity) => new()
    {
        Title = entity.Title,
        Link = $"/public/elections/{entity.Slug}",
        Description = $"{entity.Type} · {entity.Status} · Voting: {entity.VotingStartsAt:yyyy-MM-dd HH:mm} – {entity.VotingEndsAt:yyyy-MM-dd HH:mm} UTC",
        PublishedAt = entity.CreatedAt,
        Guid = entity.Id.ToString(),
        GuidIsPermaLink = false,
        Categories = [entity.Type.ToString(), entity.Status.ToString()]
    };
}
