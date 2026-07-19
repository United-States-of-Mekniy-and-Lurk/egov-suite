using System.Security.Claims;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;

namespace CitizenService.Web.Services;

/// <summary>
/// Extracts Keycloak realm roles from the JWT realm_access claim
/// and maps them to standard ClaimTypes.Role claims so that
/// [Authorize(Roles = "...")] and User.IsInRole() work naturally.
/// </summary>
public class KeycloakClaimsTransformation : IClaimsTransformation
{
    public static void AddRolesFromAccessToken(ClaimsPrincipal? principal, string? accessToken)
    {
        if (principal?.Identity is not ClaimsIdentity identity || string.IsNullOrWhiteSpace(accessToken))
            return;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return;

        var token = handler.ReadJwtToken(accessToken);
        using var payload = JsonDocument.Parse(token.Payload.SerializeToJson());
        AddRealmRoles(identity, payload.RootElement);
        AddClientRoles(identity, payload.RootElement);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null)
            return Task.FromResult(principal);

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess == null)
            return Task.FromResult(principal);

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            AddRealmRoles(identity, doc.RootElement, realmAccessIsRoot: true);
        }
        catch (JsonException)
        {
            // Malformed claim — skip silently
        }

        return Task.FromResult(principal);
    }

    private static void AddRealmRoles(
        ClaimsIdentity identity,
        JsonElement root,
        bool realmAccessIsRoot = false)
    {
        var realmAccess = root;
        if (!realmAccessIsRoot &&
            (!root.TryGetProperty("realm_access", out realmAccess) ||
             realmAccess.ValueKind != JsonValueKind.Object))
        {
            return;
        }

        if (!realmAccess.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            return;

        foreach (var role in roles.EnumerateArray())
            AddRole(identity, role.GetString());
    }

    private static void AddClientRoles(ClaimsIdentity identity, JsonElement root)
    {
        if (!root.TryGetProperty("resource_access", out var resourceAccess) ||
            resourceAccess.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var client in resourceAccess.EnumerateObject())
        {
            if (!client.Value.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var role in roles.EnumerateArray())
                AddRole(identity, role.GetString());
        }
    }

    private static void AddRole(ClaimsIdentity identity, string? roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
    }
}
