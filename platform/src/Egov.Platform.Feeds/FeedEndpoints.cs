using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Egov.Platform.Feeds;

/// <summary>
/// ASP.NET Core integration for RSS feeds. Registers feed providers and maps endpoints.
/// </summary>
public static class FeedEndpoints
{
    /// <summary>
    /// Adds a feed provider to the DI container.
    /// </summary>
    public static IServiceCollection AddFeedProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IFeedProvider
    {
        services.AddSingleton<IFeedProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Adds a scoped feed provider to the DI container (use when the provider depends on scoped services like DbContext).
    /// </summary>
    public static IServiceCollection AddScopedFeedProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IFeedProvider
    {
        services.AddScoped<IFeedProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Maps RSS feed endpoints at the given route prefix.
    /// Each registered <see cref="IFeedProvider"/> becomes available at {prefix}/{feedId}.
    /// A feed index listing all available feeds is served at {prefix}.
    /// </summary>
    public static IEndpointRouteBuilder MapRssFeeds(this IEndpointRouteBuilder endpoints, string prefix = "/feeds")
    {
        var normalizedPrefix = prefix.TrimEnd('/');

        endpoints.MapGet(normalizedPrefix, async (HttpContext context) =>
        {
            var providers = context.RequestServices.GetServices<IFeedProvider>();
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}{normalizedPrefix}";

            var links = providers.Select(p => new
            {
                p.FeedId,
                Title = p.Channel.Title,
                Url = $"{baseUrl}/{p.FeedId}"
            });

            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(links);
        });

        endpoints.MapGet($"{normalizedPrefix}/{{feedId}}", async (string feedId, HttpContext context, CancellationToken ct) =>
        {
            var providers = context.RequestServices.GetServices<IFeedProvider>();
            var provider = providers.FirstOrDefault(p =>
                p.FeedId.Equals(feedId, StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var maxItems = 50;
            if (context.Request.Query.TryGetValue("limit", out var limitStr)
                && int.TryParse(limitStr, out var limit)
                && limit is > 0 and <= 200)
            {
                maxItems = limit;
            }

            var items = await provider.GetItemsAsync(maxItems, ct);
            var selfUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
            var xml = RssSerializer.Serialize(provider.Channel, items, selfUrl);

            context.Response.ContentType = "application/rss+xml; charset=utf-8";
            context.Response.Headers["Cache-Control"] = "public, max-age=300";
            await context.Response.WriteAsync(xml, ct);
        });

        return endpoints;
    }
}
