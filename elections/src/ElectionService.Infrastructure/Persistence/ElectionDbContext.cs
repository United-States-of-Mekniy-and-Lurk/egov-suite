using ElectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Infrastructure.Persistence;

public sealed class ElectionDbContext(DbContextOptions<ElectionDbContext> options) : DbContext(options)
{
    public DbSet<Election> Elections => Set<Election>();
    public DbSet<PartyList> PartyLists => Set<PartyList>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ReferendumOption> ReferendumOptions => Set<ReferendumOption>();
    public DbSet<VoterRollEntry> VoterRollEntries => Set<VoterRollEntry>();
    public DbSet<VotingInvitation> VotingInvitations => Set<VotingInvitation>();
    public DbSet<AnonymousBallot> AnonymousBallots => Set<AnonymousBallot>();
    public DbSet<ParticipationRecord> ParticipationRecords => Set<ParticipationRecord>();
    public DbSet<ElectionTransition> ElectionTransitions => Set<ElectionTransition>();
    public DbSet<ElectionResult> ElectionResults => Set<ElectionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Election>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.Property(item => item.Slug).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(300).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(5000).IsRequired();
            entity.Property(item => item.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.EligibilityMode).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.CredentialHashKeyVersion).HasMaxLength(50).IsRequired();
            entity.Property(item => item.TerritoryCode).HasMaxLength(50);
            entity.Property(item => item.HistoricalSourceReference).HasMaxLength(1000);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Elections_EligibleVoterCount", "\"EligibleVoterCount\" IS NULL OR \"EligibleVoterCount\" >= 0");
                table.HasCheckConstraint("CK_Elections_HistoricalParticipatingVoterCount", "\"HistoricalParticipatingVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" >= 0");
                table.HasCheckConstraint("CK_Elections_HistoricalInvalidBallotCount", "\"HistoricalInvalidBallotCount\" IS NULL OR \"HistoricalInvalidBallotCount\" >= 0");
                table.HasCheckConstraint("CK_Elections_HistoricalMetadata", "(\"IsHistorical\" AND \"HistoricalSourceReference\" IS NOT NULL AND \"ImportedAt\" IS NOT NULL AND \"ImportedByPersonId\" IS NOT NULL AND \"HistoricalParticipatingVoterCount\" IS NOT NULL AND \"HistoricalInvalidBallotCount\" IS NOT NULL) OR (NOT \"IsHistorical\" AND \"HistoricalSourceReference\" IS NULL AND \"ImportedAt\" IS NULL AND \"ImportedByPersonId\" IS NULL AND \"HistoricalParticipatingVoterCount\" IS NULL AND \"HistoricalInvalidBallotCount\" IS NULL)");
                table.HasCheckConstraint("CK_Elections_HistoricalParticipationWithinEligible", "\"EligibleVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" IS NULL OR \"HistoricalParticipatingVoterCount\" <= \"EligibleVoterCount\"");
            });
        });

        modelBuilder.Entity<PartyList>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.PartyOrganizationId }).IsUnique();
            entity.HasIndex(item => new { item.ElectionId, item.SortOrder }).IsUnique();
            entity.Property(item => item.PartyRegistrationNumber).HasMaxLength(100).IsRequired();
            entity.Property(item => item.PartyName).HasMaxLength(300).IsRequired();
            entity.Property(item => item.ListName).HasMaxLength(300).IsRequired();
            entity.HasOne(item => item.Election).WithMany(item => item.PartyLists).HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.PartyListId, item.Position }).IsUnique();
            entity.HasIndex(item => new { item.PartyListId, item.PersonId }).IsUnique().HasFilter("\"PersonId\" IS NOT NULL");
            entity.Property(item => item.DisplayName).HasMaxLength(300).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.HasOne(item => item.PartyList).WithMany(item => item.Candidates).HasForeignKey(item => item.PartyListId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReferendumOption>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.Code }).IsUnique();
            entity.HasIndex(item => new { item.ElectionId, item.SortOrder }).IsUnique();
            entity.Property(item => item.Code).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(300).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.HasOne(item => item.Election).WithMany(item => item.ReferendumOptions).HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoterRollEntry>(entity =>
        {
            entity.HasKey(item => new { item.ElectionId, item.PersonId });
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VotingInvitation>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.TokenHash }).IsUnique();
            entity.Property(item => item.TokenHash).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(300);
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnonymousBallot>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.SelectionId, item.TerritoryCode });
            entity.Property(item => item.SelectionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.TerritoryCode).HasMaxLength(50);
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParticipationRecord>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.Channel, item.CredentialHash }).IsUnique();
            entity.Property(item => item.Channel).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.CredentialHash).HasMaxLength(100).IsRequired();
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ElectionTransition>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.ChangedAt });
            entity.Property(item => item.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.Reason).HasMaxLength(2000);
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ElectionResult>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ElectionId, item.SelectionId, item.TerritoryCode }).IsUnique();
            entity.Property(item => item.SelectionType).HasConversion<string>().HasMaxLength(30);
            entity.Property(item => item.SelectionLabel).HasMaxLength(300).IsRequired();
            entity.Property(item => item.TerritoryCode).HasMaxLength(50);
            entity.ToTable(table => table.HasCheckConstraint("CK_ElectionResults_VoteCount", "\"VoteCount\" >= 0"));
            entity.HasOne<Election>().WithMany().HasForeignKey(item => item.ElectionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}