namespace Gov.Cli.Core;

public static class PlanRenderer
{
    public static string Render(PlanResult plan)
    {
        var lines = new List<string>();

        RenderSection(lines, "Clients to create", plan.ClientsToCreate.Select(c => c.LogicalName));
        RenderSection(lines, "Clients to update", plan.ClientsToUpdate.Select(c => c.LogicalName));
        RenderSection(lines, "Clients to delete", plan.ClientsToDelete.Select(c => c.LogicalName));
        RenderSection(lines, "Roles to create", plan.RolesToCreate.Select(r => r.Name));
        RenderSection(lines, "Roles to delete", plan.RolesToDelete.Select(r => r.Name));

        if (!plan.HasChanges)
        {
            lines.Add("No changes.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void RenderSection(List<string> output, string title, IEnumerable<string> items)
    {
        var list = items.Order(StringComparer.Ordinal).ToList();
        output.Add($"{title}:");
        if (list.Count == 0)
        {
            output.Add("  - (none)");
            return;
        }

        foreach (var item in list)
        {
            output.Add($"  - {item}");
        }
    }
}
