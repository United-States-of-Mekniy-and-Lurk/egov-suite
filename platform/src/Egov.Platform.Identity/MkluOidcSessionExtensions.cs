using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Egov.Platform.Identity;

public static class MkluOidcSessionExtensions
{
    public static IServiceCollection AddMkluOidcSessionManagement(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<OidcAccessTokenService>();
        return services;
    }

    public static RouteHandlerBuilder MapMkluOidcSessionKeepalive(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/session/keepalive") =>
        endpoints.MapGet(pattern, HandleKeepaliveAsync)
            .RequireAuthorization();

    private static async Task<IResult> HandleKeepaliveAsync(
        HttpContext context,
        OidcAccessTokenService tokens,
        CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return string.IsNullOrWhiteSpace(await tokens.GetAccessTokenAsync(ct))
            ? Results.Unauthorized()
            : Results.NoContent();
    }
}