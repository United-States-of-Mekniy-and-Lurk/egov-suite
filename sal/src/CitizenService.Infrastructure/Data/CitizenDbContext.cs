using CitizenService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CitizenService.Infrastructure.Data;

public class CitizenDbContext : DbContext
{
    public CitizenDbContext(DbContextOptions<CitizenDbContext> options) : base(options) { }

    public DbSet<Citizen> Citizens { get; set; }
    public DbSet<CitizenshipApplication> Applications { get; set; }
    public DbSet<ApplicationTransition> ApplicationTransitions { get; set; }
    public DbSet<ApplicationForm> ApplicationForms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Citizen>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PersonId).IsUnique();
            e.HasIndex(x => x.CitizenNumber).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<CitizenshipApplication>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();

            var converter = new ValueConverter<JsonDocument?, string?>(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v));

            e.Property(x => x.FormAnswers)
                .HasColumnType("jsonb")
                .HasConversion(converter);
        });

        modelBuilder.Entity<ApplicationTransition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ApplicationId);
            e.Property(x => x.FromStatus).HasConversion<string>();
            e.Property(x => x.ToStatus).HasConversion<string>();
        });

        modelBuilder.Entity<ApplicationForm>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Name, x.Version }).IsUnique();
        });

        modelBuilder.Entity<ApplicationForm>().HasData(new ApplicationForm
        {
            Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            Name = "citizenship_application",
            Version = 1,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DefinitionJson = """{"title":"Citizenship Application","fields":[{"name":"legal_name","type":"text","label":"Legal Name","required":true},{"name":"motivation","type":"textarea","label":"Motivation","required":true},{"name":"date_of_birth","type":"date","label":"Date of Birth","required":true}]}"""
        });
    }
}
