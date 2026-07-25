using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ElectionService.Infrastructure.Persistence;

public sealed class DesignTimeElectionDbContextFactory : IDesignTimeDbContextFactory<ElectionDbContext>
{
    public ElectionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=elections;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<ElectionDbContext>().UseNpgsql(connectionString).Options;
        return new ElectionDbContext(options);
    }
}