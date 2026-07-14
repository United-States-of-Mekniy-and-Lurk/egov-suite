namespace Gov.Cli.Manifest;

public sealed class ServiceManifest
{
    public string Service { get; init; } = string.Empty;
    public AuthSection Auth { get; init; } = new();
    public List<string> Roles { get; init; } = [];
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
