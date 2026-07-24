using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrganizationRegistry.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrganizationRegistryDbContext>
{
    public OrganizationRegistryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=organization_registry;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<OrganizationRegistryDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new OrganizationRegistryDbContext(options);
    }
}