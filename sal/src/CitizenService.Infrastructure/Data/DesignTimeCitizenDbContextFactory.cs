using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CitizenService.Infrastructure.Data;

public class DesignTimeCitizenDbContextFactory : IDesignTimeDbContextFactory<CitizenDbContext>
{
    public CitizenDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CitizenDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=citizenservice;Username=postgres;******");
        return new CitizenDbContext(optionsBuilder.Options);
    }
}
