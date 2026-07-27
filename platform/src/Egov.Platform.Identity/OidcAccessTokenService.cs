using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
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
    private static readonly TimeSpan RefreshHandoffLifetime = TimeSpan.FromMinutes(2);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly Dictionary<string, RefreshResult> _refreshHandoffs = new(StringComparer.Ordinal);

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

        var refreshTokenKey = HashToken(refreshToken);
        if (TryGetRefreshHandoff(refreshTokenKey, out var handoff))
        {
            await StoreRefreshResultAsync(context, authentication, handoff);
            logger.LogDebug("Reused a concurrent OIDC token refresh result");
            return handoff.AccessToken;
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
            var oauthError = await ReadOAuthErrorAsync(response, ct);
            logger.LogWarning(
                "OIDC token refresh failed with status {StatusCode} error={Error}",
                response.StatusCode,
                oauthError ?? "unknown");
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

        var refreshedToken = payload.RootElement.TryGetProperty("refresh_token", out var refreshTokenProperty)
            ? refreshTokenProperty.GetString()
            : null;
        int? expiresIn = payload.RootElement.TryGetProperty("expires_in", out var expiresInProperty) &&
            expiresInProperty.TryGetInt32(out var parsedExpiresIn)
                ? parsedExpiresIn
                : null;
        var result = new RefreshResult(accessToken, refreshedToken, expiresIn, DateTimeOffset.UtcNow);
        _refreshHandoffs[refreshTokenKey] = result;
        RemoveExpiredRefreshHandoffs(result.RefreshedAt);
        await StoreRefreshResultAsync(context, authentication, result);
        logger.LogInformation("Refreshed the OIDC access token");
        return accessToken;
    }

    private static async Task StoreRefreshResultAsync(
        HttpContext context,
        AuthenticateResult authentication,
        RefreshResult result)
    {
        var properties = authentication.Properties!;
        var tokens = properties.GetTokens().ToList();
        SetToken(tokens, "access_token", result.AccessToken);
        SetToken(tokens, "refresh_token", result.RefreshToken);
        if (result.ExpiresIn.HasValue)
        {
            SetToken(tokens, "expires_at", result.RefreshedAt.AddSeconds(result.ExpiresIn.Value)
                .ToString("o", CultureInfo.InvariantCulture));
        }

        properties.StoreTokens(tokens);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authentication.Principal!,
            properties);
    }

    private bool TryGetRefreshHandoff(string refreshTokenKey, out RefreshResult result)
    {
        if (_refreshHandoffs.TryGetValue(refreshTokenKey, out result!) &&
            DateTimeOffset.UtcNow - result.RefreshedAt <= RefreshHandoffLifetime)
        {
            return true;
        }

        _refreshHandoffs.Remove(refreshTokenKey);
        return false;
    }

    private void RemoveExpiredRefreshHandoffs(DateTimeOffset now)
    {
        foreach (var key in _refreshHandoffs
                     .Where(entry => now - entry.Value.RefreshedAt > RefreshHandoffLifetime)
                     .Select(entry => entry.Key)
                     .ToArray())
        {
            _refreshHandoffs.Remove(key);
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed record RefreshResult(
        string AccessToken,
        string? RefreshToken,
        int? ExpiresIn,
        DateTimeOffset RefreshedAt);

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

    private static async Task<string?> ReadOAuthErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            return payload.RootElement.TryGetProperty("error", out var errorProperty)
                ? errorProperty.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
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
