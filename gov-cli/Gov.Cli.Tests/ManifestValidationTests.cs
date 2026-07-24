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

    [Fact]
    public void Validate_AllowsManifestWithoutPortalMetadata()
    {
        var manifest = new ServiceManifest
        {
            Service = "citizen-service",
            Auth = new AuthSection
            {
                Clients = new Dictionary<string, AuthClientManifest>
                {
                    ["web"] = new() { RedirectUris = ["https://citizen.example.test/signin-oidc"] },
                },
            },
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenPortalMetadataIsInvalid()
    {
        var manifest = new ServiceManifest
        {
            Service = "citizen-service",
            Portal = new PortalSection
            {
                Url = "javascript:alert(1)",
                Keywords = ["citizenship", ""],
            },
            Auth = new AuthSection
            {
                Clients = new Dictionary<string, AuthClientManifest>
                {
                    ["web"] = new() { RedirectUris = ["https://citizen.example.test/signin-oidc"] },
                },
            },
        };

        var errors = ManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("'portal.name' is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("'portal.description' is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("'portal.url' must be", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("'portal.category' is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("'portal.keywords' cannot", StringComparison.Ordinal));
    }
}
