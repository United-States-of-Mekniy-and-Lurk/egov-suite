using System.Text.Json;
using GovernmentPortal.Web.Models;

namespace GovernmentPortal.Web.Services;

public sealed class ServiceCatalog
{
    private readonly IReadOnlyList<ServiceEntry> _services;

    public ServiceCatalog(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["Catalog:Path"] ?? "catalog/services.json";
        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        if (!File.Exists(path))
        {
            _services = [];
            return;
        }

        var json = File.ReadAllText(path);
        _services = JsonSerializer.Deserialize<List<ServiceEntry>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    public IReadOnlyList<ServiceEntry> GetPublicServices() =>
        _services.Where(service => service.Public)
            .OrderBy(service => service.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public ServiceEntry? Find(string service) =>
        _services.FirstOrDefault(entry => string.Equals(entry.Service, service, StringComparison.Ordinal));
}