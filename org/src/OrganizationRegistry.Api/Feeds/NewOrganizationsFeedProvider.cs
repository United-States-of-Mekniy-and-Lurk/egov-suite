using Egov.Platform.Feeds;
using OrganizationRegistry.Application.Abstractions;

namespace OrganizationRegistry.Api.Feeds;

public sealed class NewOrganizationsFeedProvider(IOrganizationRegistryStore store) : FeedProvider<Domain.Entities.Organization>
{
    public override string FeedId => "new-organizations";

    public override RssFeedChannel Channel => new()
    {
        Title = "New Organizations — Organization Registry",
        Link = "/feeds/new-organizations",
        Description = "Recently registered organizations in the public registry.",
        Language = "en",
        TimeToLiveMinutes = 15
    };

    public override async Task<IReadOnlyList<Domain.Entities.Organization>> GetSourceEntitiesAsync(int maxItems, CancellationToken ct)
    {
        return await store.ListPublicOrganizationsAsync(null, null, 0, maxItems, ct);
    }

    public override RssFeedItem MapToItem(Domain.Entities.Organization entity) => new()
    {
        Title = entity.LegalName,
        Link = $"/public/organizations/{entity.Slug}",
        Description = $"{entity.LegalFormCode} · {entity.Purpose}",
        PublishedAt = entity.RegisteredAt,
        Guid = entity.Id.ToString(),
        GuidIsPermaLink = false,
        Categories = [$"Legal form: {entity.LegalFormCode}"]
    };
}
