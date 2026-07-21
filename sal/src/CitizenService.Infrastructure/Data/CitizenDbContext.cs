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
    public DbSet<ApplicationFormDraft> ApplicationFormDrafts { get; set; }
    public DbSet<RegistryFieldDefinition> RegistryFieldDefinitions { get; set; }
    public DbSet<CitizenFieldValue> CitizenFieldValues { get; set; }
    public DbSet<FieldCorrectionRequest> FieldCorrectionRequests { get; set; }

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

        modelBuilder.Entity<ApplicationFormDraft>(e =>
        {
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(128);
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

        modelBuilder.Entity<RegistryFieldDefinition>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.LabelsJson).HasColumnType("jsonb");
            e.Property(x => x.FieldType).HasConversion<string>();
            e.Property(x => x.OptionSourceType).HasConversion<string>();
            e.Property(x => x.StaticOptionsJson).HasColumnType("jsonb");
            e.Property(x => x.OptionSourceService).HasMaxLength(128);
            e.Property(x => x.OptionSourcePath).HasMaxLength(512);
        });

        modelBuilder.Entity<CitizenFieldValue>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CitizenId, x.FieldDefinitionId, x.ValidFrom });
            e.HasIndex(x => new { x.CitizenId, x.FieldDefinitionId })
                .IsUnique()
                .HasFilter("\"ValidTo\" IS NULL");
            e.HasOne<Citizen>()
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<RegistryFieldDefinition>()
                .WithMany()
                .HasForeignKey(x => x.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CitizenshipApplication>()
                .WithMany()
                .HasForeignKey(x => x.SourceApplicationId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<FieldCorrectionRequest>()
                .WithMany()
                .HasForeignKey(x => x.SourceCorrectionRequestId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FieldCorrectionRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.CitizenId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.CitizenId, x.FieldDefinitionId })
                .IsUnique()
                .HasFilter("\"Status\" = 'Submitted'");
            e.HasOne<Citizen>()
                .WithMany()
                .HasForeignKey(x => x.CitizenId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<RegistryFieldDefinition>()
                .WithMany()
                .HasForeignKey(x => x.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
