namespace Gov.Cli.Manifest;

public static class ManifestValidator
{
    public static IReadOnlyList<string> Validate(ServiceManifest manifest)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.Service))
        {
            errors.Add("'service' is required.");
        }

        if (manifest.Auth is null)
        {
            errors.Add("'auth' is required.");
            return errors;
        }

        if (manifest.Auth.Clients is null)
        {
            errors.Add("'auth.clients' is required.");
        }
        else if (manifest.Auth.Clients.Count == 0)
        {
            errors.Add("'auth.clients' must include at least one client.");
        }
        else
        {
            foreach (var (name, client) in manifest.Auth.Clients)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add("Client key cannot be empty.");
                }

                if (client is null)
                {
                    errors.Add($"Client '{name}' is null.");
                    continue;
                }

                if (client.RedirectUris is null || client.RedirectUris.Count == 0)
                {
                    errors.Add($"Client '{name}' requires at least one redirect URI.");
                }
                else
                {
                    var uriDuplicates = client.RedirectUris
                        .Where(static x => !string.IsNullOrWhiteSpace(x))
                        .GroupBy(x => x, StringComparer.Ordinal)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    foreach (var duplicate in uriDuplicates)
                    {
                        errors.Add($"Client '{name}' has duplicate redirect URI '{duplicate}'.");
                    }

                    if (client.RedirectUris.Any(string.IsNullOrWhiteSpace))
                    {
                        errors.Add($"Client '{name}' has empty redirect URI values.");
                    }
                }

                if (client.Scopes is not null)
                {
                    if (client.Scopes.Any(string.IsNullOrWhiteSpace))
                    {
                        errors.Add($"Client '{name}' has empty scope values.");
                    }

                    var scopeDuplicates = client.Scopes
                        .Where(static x => !string.IsNullOrWhiteSpace(x))
                        .GroupBy(x => x, StringComparer.Ordinal)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    foreach (var duplicate in scopeDuplicates)
                    {
                        errors.Add($"Client '{name}' has duplicate scope '{duplicate}'.");
                    }
                }
            }
        }

        if (manifest.Roles is not null)
        {
            if (manifest.Roles.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("'roles' cannot contain empty values.");
            }

            var roleDuplicates = manifest.Roles
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in roleDuplicates)
            {
                errors.Add($"Duplicate role '{duplicate}'.");
            }
        }

        return errors;
    }
}
