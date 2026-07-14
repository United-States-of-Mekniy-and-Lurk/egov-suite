using Gov.Cli.Manifest;

namespace Gov.Cli.Tests;

public class ManifestValidationTests
{
    [Fact]
    public void Validate_ReturnsErrors_WhenServiceMissingAndClientInvalid()
    {
        var manifest = new ServiceManifest
        {
            Service = "",
            Auth = new AuthSection
            {
                Clients = new Dictionary<string, AuthClientManifest>
                {
                    ["web"] = new()
                    {
                        RedirectUris = [],
                        Scopes = ["", "citizen.read", "citizen.read"],
                    },
                },
            },
            Roles = ["admin", "", "admin"],
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, e => e.Contains("'service' is required", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("requires at least one redirect URI", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("empty scope values", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("Duplicate role 'admin'", StringComparison.Ordinal));
    }
}
