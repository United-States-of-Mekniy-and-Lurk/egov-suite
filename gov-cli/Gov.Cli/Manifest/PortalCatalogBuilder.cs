using System.Text.Json;

namespace Gov.Cli.Manifest;

public static class PortalCatalogBuilder
{
    public static (string? Json, List<string> Errors) Build(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return (null, [$"Manifest directory does not exist: {directory}"]);
        }

        var entries = new List<PortalCatalogEntry>();
        var errors = new List<string>();

        foreach (var path in Directory.EnumerateFiles(directory, "*.gov.yaml", SearchOption.AllDirectories)
                     .Order(StringComparer.Ordinal))
        {
            var (manifest, manifestErrors) = ManifestLoader.Load(path);
            if (manifestErrors.Count > 0 || manifest is null)
            {
                errors.AddRange(manifestErrors.Select(error => $"{path}: {error}"));
                continue;
            }

            if (manifest.Portal is null)
            {
                continue;
            }

            entries.Add(new PortalCatalogEntry(
                manifest.Service,
                manifest.Portal.Name,
                manifest.Portal.Description,
                manifest.Portal.Url,
                manifest.Portal.Category,
                manifest.Portal.Keywords,
                manifest.Portal.Localizations.ToDictionary(
                    pair => pair.Key,
                    pair => new PortalCatalogLocalization(
                        pair.Value.Name,
                        pair.Value.Description,
                        pair.Value.Category,
                        pair.Value.Keywords),
                    StringComparer.OrdinalIgnoreCase),
                manifest.Portal.Public));
        }

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        var json = JsonSerializer.Serialize(
            entries.OrderBy(entry => entry.Name, StringComparer.Ordinal),
            new JsonSerializerOptions { WriteIndented = true });
        return (json, errors);
    }
}

public sealed record PortalCatalogEntry(
    string Service,
    string Name,
    string Description,
    string Url,
    string Category,
    IReadOnlyList<string> Keywords,
    IReadOnlyDictionary<string, PortalCatalogLocalization> Localizations,
    bool Public);

public sealed record PortalCatalogLocalization(
    string Name,
    string Description,
    string Category,
    IReadOnlyList<string> Keywords);