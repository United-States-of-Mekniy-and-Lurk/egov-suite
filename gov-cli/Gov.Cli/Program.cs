using Gov.Cli.Core;
using Gov.Cli.Keycloak;
using Gov.Cli.Manifest;

return await GovProgram.RunAsync(args);

public static class GovProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: gov <validate|plan|apply> <manifest.yaml>");
            Console.Error.WriteLine("       gov catalog <manifest-directory>");
            return 1;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var inputPath = Path.GetFullPath(args[1]);

        if (command == "catalog")
        {
            var (json, catalogErrors) = PortalCatalogBuilder.Build(inputPath);
            if (catalogErrors.Count > 0 || json is null)
            {
                PrintErrors(catalogErrors);
                return 1;
            }

            Console.WriteLine(json);
            return 0;
        }

        var (manifest, errors) = ManifestLoader.Load(inputPath);
        if (errors.Count > 0 || manifest is null)
        {
            PrintErrors(errors);
            return 1;
        }

        if (command == "validate")
        {
            Console.WriteLine("Manifest is valid.");
            return 0;
        }

        if (command is not ("plan" or "apply"))
        {
            Console.Error.WriteLine($"Unsupported command '{command}'. Expected validate, plan, or apply.");
            return 1;
        }

        var (options, optionErrors) = KeycloakOptions.FromEnvironment();
        if (optionErrors.Count > 0 || options is null)
        {
            PrintErrors(optionErrors);
            return 1;
        }

        using var httpClient = new HttpClient();
        var adapter = new KeycloakAdapter(httpClient, options);

        var desired = DesiredState.FromManifest(manifest);
        var current = await adapter.GetCurrentStateAsync(manifest.Service);
        var plan = DiffEngine.BuildPlan(desired, current);

        Console.WriteLine(PlanRenderer.Render(plan));

        if (command == "plan")
        {
            return 0;
        }

        await ApplyEngine.ApplyAsync(adapter, manifest.Service, plan);
        Console.WriteLine("Apply complete.");
        return 0;
    }

    private static void PrintErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"ERROR: {error}");
        }
    }
}
