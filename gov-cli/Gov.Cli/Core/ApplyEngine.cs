namespace Gov.Cli.Core;

public static class ApplyEngine
{
    public static async Task ApplyAsync(IPlatformAdapter adapter, string service, PlanResult plan, CancellationToken cancellationToken = default)
    {
        foreach (var role in plan.RolesToCreate)
        {
            await adapter.CreateRoleAsync(service, role, cancellationToken);
        }

        foreach (var role in plan.RolesToDelete)
        {
            await adapter.DeleteRoleAsync(service, role, cancellationToken);
        }

        foreach (var client in plan.ClientsToCreate)
        {
            await adapter.CreateClientAsync(service, client, cancellationToken);
        }

        foreach (var client in plan.ClientsToUpdate)
        {
            await adapter.UpdateClientAsync(service, client, cancellationToken);
        }

        foreach (var client in plan.ClientsToDelete)
        {
            await adapter.DeleteClientAsync(client, cancellationToken);
        }
    }
}
