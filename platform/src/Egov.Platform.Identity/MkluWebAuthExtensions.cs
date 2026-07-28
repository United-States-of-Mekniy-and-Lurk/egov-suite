using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Egov.Platform.Identity;

public sealed class MkluWebAuthOptions
{
    /// <summary>
    /// When true, validates that the access token contains the audience specified in <c>Jwt:Audience</c>.
    /// Default: true.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
}

public static class MkluWebAuthExtensions
{
    /// <summary>
    /// Adds Cookie + OpenID Connect authentication with Keycloak-compatible defaults,
    /// role extraction from the access token, and the standard Bearer token handler.
    /// <para>Configuration sections: <c>Oidc:Authority</c>, <c>Oidc:ClientId</c>, <c>Oidc:ClientSecret</c>,
    /// <c>Oidc:PublicBaseUrl</c> (optional), <c>Oidc:RequireHttpsMetadata</c> (default true),
    /// <c>Jwt:Audience</c> (optional audience validation).</para>
    /// </summary>
    public static IServiceCollection AddMkluWebAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MkluWebAuthOptions>? configure = null)
    {
        var options = new MkluWebAuthOptions();
        configure?.Invoke(options);

        services.AddAuthentication(auth =>
        {
            auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(cookie =>
        {
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SameSite = SameSiteMode.Lax;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        })
        .AddOpenIdConnect(oidc =>
        {
            oidc.Authority = configuration["Oidc:Authority"];
            oidc.ClientId = configuration["Oidc:ClientId"];
            oidc.ClientSecret = configuration["Oidc:ClientSecret"];
            oidc.RequireHttpsMetadata = configuration.GetValue("Oidc:RequireHttpsMetadata", true);
            oidc.ResponseType = "code";
            oidc.ResponseMode = "query";
            oidc.SaveTokens = true;
            oidc.GetClaimsFromUserInfoEndpoint = true;
            oidc.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
            oidc.CorrelationCookie.SameSite = SameSiteMode.Lax;
            oidc.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            oidc.NonceCookie.SameSite = SameSiteMode.Lax;
            oidc.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            oidc.Scope.Add("openid");
            oidc.Scope.Add("profile");
            oidc.Scope.Add("email");

            var requiredAudience = options.ValidateAudience ? configuration["Jwt:Audience"] : null;
            var publicBaseUrl = configuration["Oidc:PublicBaseUrl"]?.TrimEnd('/');

            oidc.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = context =>
                {
                    var accessToken = context.TokenEndpointResponse?.AccessToken;
                    if (!string.IsNullOrWhiteSpace(requiredAudience) && !string.IsNullOrWhiteSpace(accessToken))
                    {
                        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                        if (!token.Audiences.Contains(requiredAudience, StringComparer.Ordinal))
                            throw new AuthenticationFailureException(
                                $"The access token does not contain the required audience '{requiredAudience}'.");
                    }
                    KeycloakClaimsTransformation.AddRolesFromAccessToken(context.Principal, accessToken);
                    var roles = context.Principal?.FindAll(ClaimTypes.Role)
                        .Select(claim => claim.Value).OrderBy(role => role).ToArray() ?? [];
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("OpenIdConnect")
                        .LogInformation("OIDC token validated with roles [{Roles}]", string.Join(", ", roles));
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProvider = context =>
                {
                    if (!string.IsNullOrWhiteSpace(publicBaseUrl))
                        context.ProtocolMessage.RedirectUri = $"{publicBaseUrl}{oidc.CallbackPath}";
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("OpenIdConnect")
                        .LogInformation("OIDC challenge authority={Authority} redirectUri={RedirectUri}",
                            oidc.Authority, context.ProtocolMessage.RedirectUri);
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    if (!string.IsNullOrWhiteSpace(publicBaseUrl))
                        context.ProtocolMessage.PostLogoutRedirectUri = $"{publicBaseUrl}/";
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("OpenIdConnect")
                        .LogWarning(context.Failure, "OIDC remote failure");
                    context.HandleResponse();
                    context.Response.Redirect("/Error");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddMemoryCache();
        services.AddMkluOidcSessionManagement();
        services.AddTransient<MkluBearerTokenHandler>();
        return services;
    }
}

/// <summary>
/// Delegating handler that injects the current user's access token into outgoing HTTP requests
/// to downstream APIs. Register as a transient and add to HttpClient pipelines.
/// </summary>
public sealed class MkluBearerTokenHandler(OidcAccessTokenService accessTokenService, ILogger<MkluBearerTokenHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await accessTokenService.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var serviceName = request.RequestUri?.Host ?? "downstream";
            var tokenInfo = "unavailable";
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(accessToken))
                {
                    var token = handler.ReadJwtToken(accessToken);
                    tokenInfo = $"issuer={token.Issuer}, audiences=[{string.Join(", ", token.Audiences)}], expires={token.ValidTo:O}";
                }
            }
            logger.LogWarning("Downstream 401 to {RequestUri}; token {TokenInfo}", request.RequestUri, tokenInfo);
        }
        return response;
    }
}
