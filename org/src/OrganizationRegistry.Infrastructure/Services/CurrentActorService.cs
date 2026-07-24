using System.Security.Claims;
using System.Text.Json;
using Egov.Platform.Identity;
using Microsoft.AspNetCore.Http;

namespace OrganizationRegistry.Infrastructure.Services;

public sealed class CurrentActorService(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public Guid PersonId => Guid.TryParse(
        httpContextAccessor.HttpContext?.User.FindFirst("person_id")?.Value,
        out var personId) ? personId : Guid.Empty;

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null) return [];
            var roleClaims = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
            if (roleClaims.Count > 0) return roleClaims;
            var realmAccess = user.FindFirst("realm_access")?.Value;
            if (string.IsNullOrWhiteSpace(realmAccess)) return [];
            try
            {
                using var document = JsonDocument.Parse(realmAccess);
                return document.RootElement.TryGetProperty("roles", out var roles)
                    ? roles.EnumerateArray().Select(role => role.GetString()).OfType<string>().ToList()
                    : [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}