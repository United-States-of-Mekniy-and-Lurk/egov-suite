using Ego.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ego.Infrastructure.Persistence;

public class EgoDbContext(DbContextOptions<EgoDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EgoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
