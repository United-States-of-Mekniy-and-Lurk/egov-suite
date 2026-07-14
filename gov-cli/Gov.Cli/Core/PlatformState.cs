using Gov.Cli.Manifest;

namespace Gov.Cli.Core;

public sealed record DesiredClient(string LogicalName, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> Scopes);

public sealed record DesiredState(string Service, IReadOnlyList<DesiredClient> Clients, IReadOnlyList<string> Roles)
{
    public static DesiredState FromManifest(ServiceManifest manifest)
    {
        var clients = manifest.Auth.Clients
            .Select(kvp => new DesiredClient(
                kvp.Key,
                kvp.Value.RedirectUris.Order(StringComparer.Ordinal).ToArray(),
                kvp.Value.Scopes.Order(StringComparer.Ordinal).ToArray()))
            .OrderBy(c => c.LogicalName, StringComparer.Ordinal)
            .ToArray();

        var roles = (manifest.Roles ?? [])
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new DesiredState(manifest.Service, clients, roles);
    }
}

public sealed record CurrentClient(string LogicalName, string KeycloakId, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> Scopes);

public sealed record CurrentState(IReadOnlyList<CurrentClient> Clients, IReadOnlyList<string> Roles);

public sealed record ClientCreate(string LogicalName, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> Scopes);

public sealed record ClientUpdate(string LogicalName, string KeycloakId, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> Scopes);

public sealed record ClientDelete(string LogicalName, string KeycloakId);

public sealed record RoleCreate(string Name);

public sealed record RoleDelete(string Name);

public sealed record PlanResult(
    IReadOnlyList<ClientCreate> ClientsToCreate,
    IReadOnlyList<ClientUpdate> ClientsToUpdate,
    IReadOnlyList<ClientDelete> ClientsToDelete,
    IReadOnlyList<RoleCreate> RolesToCreate,
    IReadOnlyList<RoleDelete> RolesToDelete)
{
    public bool HasChanges => ClientsToCreate.Count > 0 || ClientsToUpdate.Count > 0 || ClientsToDelete.Count > 0 || RolesToCreate.Count > 0 || RolesToDelete.Count > 0;
}

public interface IPlatformAdapter
{
    Task<CurrentState> GetCurrentStateAsync(string service, CancellationToken cancellationToken = default);
    Task CreateClientAsync(string service, ClientCreate client, CancellationToken cancellationToken = default);
    Task UpdateClientAsync(string service, ClientUpdate client, CancellationToken cancellationToken = default);
    Task DeleteClientAsync(ClientDelete client, CancellationToken cancellationToken = default);
    Task CreateRoleAsync(string service, RoleCreate role, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(string service, RoleDelete role, CancellationToken cancellationToken = default);
}
