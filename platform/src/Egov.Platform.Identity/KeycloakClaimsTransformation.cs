using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Egov.Platform.Identity;

public sealed class KeycloakClaimsTransformation : IClaimsTransformation
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
        AddRoles(identity, payload.RootElement);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return Task.FromResult(principal);

        AddRolesFromClaim(identity, principal.FindFirst("realm_access")?.Value, isRealmAccess: true);
        AddRolesFromClaim(identity, principal.FindFirst("resource_access")?.Value, isRealmAccess: false);
        return Task.FromResult(principal);
    }

    private static void AddRolesFromClaim(ClaimsIdentity identity, string? json, bool isRealmAccess)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (isRealmAccess)
                AddRealmRoles(identity, document.RootElement, realmAccessIsRoot: true);
            else
                AddClientRoles(identity, document.RootElement, resourceAccessIsRoot: true);
        }
        catch (JsonException)
        {
        }
    }

    private static void AddRoles(ClaimsIdentity identity, JsonElement root)
    {
        AddRealmRoles(identity, root);
        AddClientRoles(identity, root);
    }

    private static void AddRealmRoles(ClaimsIdentity identity, JsonElement root, bool realmAccessIsRoot = false)
    {
        var realmAccess = root;
        if (!realmAccessIsRoot &&
            (!root.TryGetProperty("realm_access", out realmAccess) || realmAccess.ValueKind != JsonValueKind.Object))
            return;

        if (!realmAccess.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            return;

        foreach (var role in roles.EnumerateArray()) AddRole(identity, role.GetString());
    }

    private static void AddClientRoles(ClaimsIdentity identity, JsonElement root, bool resourceAccessIsRoot = false)
    {
        var resourceAccess = root;
        if (!resourceAccessIsRoot &&
            (!root.TryGetProperty("resource_access", out resourceAccess) || resourceAccess.ValueKind != JsonValueKind.Object))
            return;

        if (resourceAccess.ValueKind != JsonValueKind.Object) return;
        foreach (var client in resourceAccess.EnumerateObject())
        {
            if (!client.Value.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var role in roles.EnumerateArray()) AddRole(identity, role.GetString());
        }
    }

    private static void AddRole(ClaimsIdentity identity, string? roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName) && !identity.HasClaim(ClaimTypes.Role, roleName))
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
    }
}