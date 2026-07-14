using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gov.Cli.Manifest;

public static class ManifestLoader
{
    public static (ServiceManifest? Manifest, List<string> Errors) Load(string path)
    {
        var errors = new List<string>();

        if (!File.Exists(path))
        {
            errors.Add($"Manifest file does not exist: {path}");
            return (null, errors);
        }

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .Build();

        try
        {
            var manifest = deserializer.Deserialize<ServiceManifest>(yaml);
            if (manifest is null)
            {
                errors.Add("Manifest is empty.");
                return (null, errors);
            }

            errors.AddRange(ManifestValidator.Validate(manifest));
            return (manifest, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse YAML: {ex.Message}");
            return (null, errors);
        }
    }
}
