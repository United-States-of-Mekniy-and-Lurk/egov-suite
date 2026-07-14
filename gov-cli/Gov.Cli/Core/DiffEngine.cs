namespace Gov.Cli.Core;

public static class DiffEngine
{
    public static PlanResult BuildPlan(DesiredState desired, CurrentState current)
    {
        var desiredClients = desired.Clients.ToDictionary(c => c.LogicalName, StringComparer.Ordinal);
        var currentClients = current.Clients.ToDictionary(c => c.LogicalName, StringComparer.Ordinal);

        var clientsToCreate = new List<ClientCreate>();
        var clientsToUpdate = new List<ClientUpdate>();
        var clientsToDelete = new List<ClientDelete>();

        foreach (var desiredClient in desired.Clients)
        {
            if (!currentClients.TryGetValue(desiredClient.LogicalName, out var currentClient))
            {
                clientsToCreate.Add(new ClientCreate(desiredClient.LogicalName, desiredClient.RedirectUris, desiredClient.Scopes));
                continue;
            }

            if (!SetEquals(desiredClient.RedirectUris, currentClient.RedirectUris) || !SetEquals(desiredClient.Scopes, currentClient.Scopes))
            {
                clientsToUpdate.Add(new ClientUpdate(desiredClient.LogicalName, currentClient.KeycloakId, desiredClient.RedirectUris, desiredClient.Scopes));
            }
        }

        foreach (var currentClient in current.Clients)
        {
            if (!desiredClients.ContainsKey(currentClient.LogicalName))
            {
                clientsToDelete.Add(new ClientDelete(currentClient.LogicalName, currentClient.KeycloakId));
            }
        }

        var desiredRoles = desired.Roles.ToHashSet(StringComparer.Ordinal);
        var currentRoles = current.Roles.ToHashSet(StringComparer.Ordinal);

        var rolesToCreate = desiredRoles
            .Except(currentRoles, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(static role => new RoleCreate(role))
            .ToArray();

        var rolesToDelete = currentRoles
            .Except(desiredRoles, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(static role => new RoleDelete(role))
            .ToArray();

        return new PlanResult(clientsToCreate, clientsToUpdate, clientsToDelete, rolesToCreate, rolesToDelete);
    }

    private static bool SetEquals(IEnumerable<string> left, IEnumerable<string> right)
    {
        return left.ToHashSet(StringComparer.Ordinal).SetEquals(right);
    }
}
