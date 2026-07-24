namespace Gov.Cli.Manifest;

public sealed class ServiceManifest
{
    public string Service { get; init; } = string.Empty;
    public PortalSection? Portal { get; init; }
    public AuthSection Auth { get; init; } = new();
    public List<string> Roles { get; init; } = [];
}

public sealed class PortalSection
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = [];
    public Dictionary<string, PortalLocalization> Localizations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public bool Public { get; init; } = true;
}

public sealed class PortalLocalization
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = [];
}

public sealed class AuthSection
{
    public Dictionary<string, AuthClientManifest> Clients { get; init; } = new(StringComparer.Ordinal);
}

public sealed class AuthClientManifest
{
    public List<string> RedirectUris { get; init; } = [];
    public List<string> Scopes { get; init; } = [];
}
