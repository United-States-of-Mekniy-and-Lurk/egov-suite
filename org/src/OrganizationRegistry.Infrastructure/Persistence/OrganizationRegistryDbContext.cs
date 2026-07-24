using Microsoft.EntityFrameworkCore;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Infrastructure.Persistence;

public sealed class OrganizationRegistryDbContext(DbContextOptions<OrganizationRegistryDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ClassificationDefinition> ClassificationDefinitions => Set<ClassificationDefinition>();
    public DbSet<OrganizationClassification> OrganizationClassifications => Set<OrganizationClassification>();
    public DbSet<RegistrationApplication> RegistrationApplications => Set<RegistrationApplication>();
    public DbSet<RegistrationTransition> RegistrationTransitions => Set<RegistrationTransition>();
    public DbSet<OrganizationAccessGrant> OrganizationAccessGrants => Set<OrganizationAccessGrant>();
    public DbSet<OrganizationCorrectionRequest> OrganizationCorrectionRequests => Set<OrganizationCorrectionRequest>();
    public DbSet<OrganizationAsset> OrganizationAssets => Set<OrganizationAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.RegistrationNumber).IsUnique();
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.HasIndex(item => item.LegalName);
            entity.Property(item => item.RegistrationNumber).HasMaxLength(32);
            entity.Property(item => item.Slug).HasMaxLength(180);
            entity.Property(item => item.LegalName).HasMaxLength(240);
            entity.Property(item => item.TradingName).HasMaxLength(240);
            entity.Property(item => item.LegalFormCode).HasMaxLength(80);
            entity.Property(item => item.ImportSourceReference).HasMaxLength(240);
            entity.Property(item => item.ImportNote).HasMaxLength(2000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<ClassificationDefinition>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Scheme, item.Code }).IsUnique();
            entity.Property(item => item.Scheme).HasMaxLength(80);
            entity.Property(item => item.Code).HasMaxLength(100);
            entity.Property(item => item.LabelEn).HasMaxLength(160);
            entity.Property(item => item.LabelCs).HasMaxLength(160);
            entity.HasData(SeedClassifications());
        });

        modelBuilder.Entity<OrganizationClassification>(entity =>
        {
            entity.HasKey(item => new { item.OrganizationId, item.DefinitionId });
            entity.HasOne(item => item.Organization).WithMany(item => item.Classifications).HasForeignKey(item => item.OrganizationId);
            entity.HasOne(item => item.Definition).WithMany().HasForeignKey(item => item.DefinitionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistrationApplication>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ApplicantPersonId, item.Status });
            entity.HasIndex(item => new { item.Status, item.SubmittedAt });
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.LegalName).HasMaxLength(240);
            entity.Property(item => item.TradingName).HasMaxLength(240);
            entity.Property(item => item.LegalFormCode).HasMaxLength(80);
            entity.Property(item => item.RequestedClassificationCodes).HasColumnType("text[]");
            entity.HasMany(item => item.Transitions).WithOne(item => item.Application).HasForeignKey(item => item.ApplicationId);
        });

        modelBuilder.Entity<RegistrationTransition>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ApplicationId, item.ChangedAt });
            entity.Property(item => item.FromStatus).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.ToStatus).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<OrganizationAccessGrant>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.PersonId, item.RoleCode });
            entity.HasIndex(item => new { item.PersonId, item.RevokedAt });
            entity.Property(item => item.RoleCode).HasMaxLength(80);
            entity.HasOne(item => item.Organization).WithMany(item => item.AccessGrants).HasForeignKey(item => item.OrganizationId);
        });

        modelBuilder.Entity<OrganizationCorrectionRequest>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.Status });
            entity.Property(item => item.FieldKey).HasMaxLength(100);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId);
        });

        modelBuilder.Entity<OrganizationAsset>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.Kind, item.Visibility });
            entity.Property(item => item.Kind).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.Visibility).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.CategoryCode).HasMaxLength(100);
            entity.Property(item => item.StorageKey).HasMaxLength(500);
            entity.Property(item => item.FileName).HasMaxLength(240);
            entity.Property(item => item.ContentType).HasMaxLength(120);
            entity.Property(item => item.Sha256).HasMaxLength(64);
            entity.HasOne(item => item.Organization).WithMany(item => item.Assets).HasForeignKey(item => item.OrganizationId);
        });
    }

    private static ClassificationDefinition[] SeedClassifications() =>
    [
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Scheme = "organization-category", Code = "business", LabelEn = "Business", LabelCs = "Podnik", SortOrder = 10 },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Scheme = "organization-category", Code = "non-profit", LabelEn = "Non-profit organization", LabelCs = "Nezisková organizace", SortOrder = 20 },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Scheme = "organization-category", Code = "political-party", LabelEn = "Political party", LabelCs = "Politická strana", SortOrder = 30 },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Scheme = "organization-category", Code = "public-body", LabelEn = "Public body", LabelCs = "Veřejný orgán", SortOrder = 40 }
    ];
}