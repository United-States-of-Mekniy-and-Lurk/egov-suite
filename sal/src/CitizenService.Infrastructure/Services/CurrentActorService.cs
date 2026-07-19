using System.Security.Claims;
using System.Text.Json;
using CitizenService.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CitizenService.Infrastructure.Services;

public class CurrentActorService : ICurrentActor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentActorService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid PersonId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("person_id")?.Value;
            if (claim != null && Guid.TryParse(claim, out var personId))
                return personId;
            return Guid.Empty;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return [];

            // Standard role claims (mapped by claims transformation or JWT middleware)
            var roleClaims = user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (roleClaims.Count > 0)
                return roleClaims;

            // Fallback: parse Keycloak realm_access claim directly
            var realmAccess = user.FindFirst("realm_access")?.Value;
            if (realmAccess == null) return [];

            try
            {
                using var doc = JsonDocument.Parse(realmAccess);
                if (doc.RootElement.TryGetProperty("roles", out var roles))
                {
                    return roles.EnumerateArray()
                        .Select(r => r.GetString()!)
                        .Where(r => r != null)
                        .ToList();
                }
            }
            catch (JsonException) { }

            return [];
        }
    }

    public bool IsInRole(string role) => Roles.Contains(role);
}
