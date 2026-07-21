using System.Security.Claims;
using Egov.Platform.Identity;
using FluentAssertions;

namespace Egov.Platform.Tests;

public sealed class KeycloakClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_AddsRealmAndClientRolesWithoutDuplicates()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", "{\"roles\":[\"staff\",\"shared\"]}"),
            new Claim("resource_access", "{\"registry\":{\"roles\":[\"admin\",\"shared\"]}}")
        ],
        "test");
        var principal = new ClaimsPrincipal(identity);

        await new KeycloakClaimsTransformation().TransformAsync(principal);

        principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Should()
            .BeEquivalentTo("staff", "shared", "admin");
    }
}