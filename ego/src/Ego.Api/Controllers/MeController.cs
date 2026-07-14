using System.Security.Claims;
using Ego.Api.Models;
using Ego.Application.Abstractions;
using Ego.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ego.Api.Controllers;

[Authorize]
[ApiController]
[Route("")]
public class MeController(IIdentitySynchronizer identitySynchronizer) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PersonDto>> GetAsync(CancellationToken ct)
    {
        var subject = FindFirstValue(ClaimTypes.NameIdentifier, "sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized();
        }

        var preferredUsername = FindFirstValue("preferred_username") ?? subject;
        var displayName = FindDisplayName() ?? preferredUsername;
        var email = FindFirstValue(ClaimTypes.Email, "email") ?? string.Empty;

        var person = await identitySynchronizer.SynchronizeAsync(
            new IdentityClaims(subject, preferredUsername, displayName, email),
            ct);

        return Ok(person.ToDto());
    }

    private string? FindDisplayName()
    {
        var name = FindFirstValue(ClaimTypes.Name, "name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var givenName = FindFirstValue(ClaimTypes.GivenName, "given_name");
        var familyName = FindFirstValue(ClaimTypes.Surname, "family_name");
        var parts = new[] { givenName, familyName }.Where(part => !string.IsNullOrWhiteSpace(part));
        var combined = string.Join(" ", parts);

        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private string? FindFirstValue(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = User.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
