using System.Security.Claims;
using Egov.Platform.Identity;
using Microsoft.AspNetCore.Http;

namespace ElectionService.Infrastructure.Services;

public sealed class CurrentActorService(IHttpContextAccessor accessor) : ICurrentActor
{
    public Guid PersonId => Guid.TryParse(accessor.HttpContext?.User.FindFirst("person_id")?.Value, out var id) ? id : Guid.Empty;
    public IReadOnlyList<string> Roles => accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(item => item.Value).ToList() ?? [];
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}