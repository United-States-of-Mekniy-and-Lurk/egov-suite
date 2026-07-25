using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Egov.Platform.Identity;

public sealed class OidcAccessTokenService(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    IConfiguration configuration,
    ILogger<OidcAccessTokenService> logger)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return null;

        var authentication = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authentication.Succeeded || authentication.Properties is null) return null;

        var accessToken = authentication.Properties.GetTokenValue("access_token");
        if (!NeedsRefresh(accessToken)) return accessToken;

        await _refreshLock.WaitAsync(ct);
        try
        {
            authentication = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            accessToken = authentication.Properties?.GetTokenValue("access_token");
            if (!NeedsRefresh(accessToken)) return accessToken;

            return await RefreshAsync(context, authentication, ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> RefreshAsync(
        HttpContext context,
        AuthenticateResult authentication,
        CancellationToken ct)
    {
        var properties = authentication.Properties;
        var refreshToken = properties?.GetTokenValue("refresh_token");
        if (properties is null || string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogInformation("The access token is expiring and no refresh token is available");
            return null;
        }

        var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var oidcConfiguration = await options.ConfigurationManager!.GetConfigurationAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, oidcConfiguration.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = options.ClientId ?? string.Empty,
                ["client_secret"] = options.ClientSecret ?? string.Empty
            })
        };

        using var response = await options.Backchannel.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OIDC token refresh failed with status {StatusCode}", response.StatusCode);
            return null;
        }

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        if (!payload.RootElement.TryGetProperty("access_token", out var accessTokenProperty)) return null;

        var accessToken = accessTokenProperty.GetString();
        if (string.IsNullOrWhiteSpace(accessToken)) return null;

        var requiredAudience = configuration["Jwt:Audience"];
        if (!HasRequiredAudience(accessToken, requiredAudience))
        {
            logger.LogWarning("The refreshed access token is missing required audience {Audience}", requiredAudience);
            return null;
        }

        var tokens = properties.GetTokens().ToList();
        SetToken(tokens, "access_token", accessToken);
        if (payload.RootElement.TryGetProperty("refresh_token", out var refreshTokenProperty))
            SetToken(tokens, "refresh_token", refreshTokenProperty.GetString());
        if (payload.RootElement.TryGetProperty("expires_in", out var expiresInProperty) &&
            expiresInProperty.TryGetInt32(out var expiresIn))
        {
            SetToken(tokens, "expires_at", DateTimeOffset.UtcNow.AddSeconds(expiresIn)
                .ToString("o", CultureInfo.InvariantCulture));
        }

        properties.StoreTokens(tokens);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authentication.Principal!,
            properties);
        logger.LogInformation("Refreshed the OIDC access token");
        return accessToken;
    }

    private static bool NeedsRefresh(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return false;

        var handler = new JwtSecurityTokenHandler();
        return handler.CanReadToken(accessToken) &&
            handler.ReadJwtToken(accessToken).ValidTo <= DateTime.UtcNow.AddMinutes(1);
    }

    private static bool HasRequiredAudience(string accessToken, string? requiredAudience)
    {
        if (string.IsNullOrWhiteSpace(requiredAudience)) return true;

        var handler = new JwtSecurityTokenHandler();
        return handler.CanReadToken(accessToken) &&
            handler.ReadJwtToken(accessToken).Audiences.Contains(requiredAudience, StringComparer.Ordinal);
    }

    private static void SetToken(List<AuthenticationToken> tokens, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var existing = tokens.Find(token => token.Name == name);
        if (existing is null)
            tokens.Add(new AuthenticationToken { Name = name, Value = value });
        else
            existing.Value = value;
    }
}
