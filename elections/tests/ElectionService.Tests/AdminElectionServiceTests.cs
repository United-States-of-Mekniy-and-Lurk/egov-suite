using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Application.Services;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Infrastructure.Persistence;
using Egov.Platform.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Tests;

public sealed class AdminElectionServiceTests
{
    [Fact]
    public async Task DeletePartyList_RejectsNonDraftElection()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, partyList) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: ElectionStatus.Published);
        var service = CreateService(database.Context);

        await Assert.ThrowsAsync<ElectionValidationException>(() =>
            service.DeletePartyListAsync(election.Id, partyList.Id, default));

        Assert.True(await database.Context.PartyLists.AnyAsync(item => item.Id == partyList.Id));
    }

    [Fact]
    public async Task BulkAddVoters_DeduplicatesInputAndSkipsExistingEntries()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(
            DateTime.UtcNow, EligibilityMode.SpecificVoterRoll, ElectionStatus.Draft);
        var existingPersonId = Guid.NewGuid();
        var newPersonId = Guid.NewGuid();
        database.Context.VoterRollEntries.Add(new VoterRollEntry
        {
            ElectionId = election.Id,
            PersonId = existingPersonId,
            AddedAt = DateTime.UtcNow,
            AddedByPersonId = Guid.NewGuid()
        });
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var added = await service.BulkAddVotersAsync(election.Id,
            new BulkVoterRollInput([existingPersonId, newPersonId, newPersonId]), default);

        Assert.Equal(1, added);
        Assert.Equal(2, await database.Context.VoterRollEntries.CountAsync(item => item.ElectionId == election.Id));
    }

    [Fact]
    public async Task RevokeInvitation_SetsRevokedAtAndListReturnsSafeView()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: ElectionStatus.Draft);
        var invitation = new VotingInvitation
        {
            Id = Guid.NewGuid(),
            ElectionId = election.Id,
            TokenHash = "secret-hash",
            Label = "Remote voter",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedByPersonId = Guid.NewGuid()
        };
        database.Context.VotingInvitations.Add(invitation);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var revoked = await service.RevokeInvitationAsync(election.Id, invitation.Id, default);
        var listed = Assert.Single(await service.ListInvitationsAsync(election.Id, default));

        Assert.NotNull(revoked.RevokedAt);
        Assert.Equal(revoked, listed);
        Assert.NotNull((await database.Context.VotingInvitations.AsNoTracking().SingleAsync()).RevokedAt);
        await Assert.ThrowsAsync<ElectionConflictException>(() =>
            service.RevokeInvitationAsync(election.Id, invitation.Id, default));
    }

    [Fact]
    public async Task CreateInvitation_AllowsPublishedElection()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: ElectionStatus.Published);
        var service = CreateService(database.Context);

        var created = await service.CreateInvitationAsync(election.Id, new InvitationInput(null, "Observer"), default);

        Assert.Equal("one-time-token", created.Token);
        Assert.Equal(1, await database.Context.VotingInvitations.CountAsync());
    }

    [Fact]
    public async Task ImportHistorical_CreatesArchivedAggregateRecordWithoutVotingRows()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var service = CreateService(database.Context);
        var input = HistoricalPartyElection(
            participatingVoterCount: 100,
            invalidBallotCount: 2,
            partyLists:
            [
                new(null, "HIST-1", "Historical Party", "National List", 1,
                    [new(null, "Archived Candidate", null, 1)], 90),
                new(Guid.NewGuid(), "HIST-2", "Zero Party", "Zero List", 2, null, 0)
            ]);

        var imported = await service.ImportHistoricalAsync(input, default);

        Assert.Equal(ElectionStatus.Archived, imported.Status);
        Assert.True(imported.IsHistorical);
        Assert.Equal(input.SourceReference, imported.HistoricalSourceReference);
        Assert.Equal(input.EligibleVoterCount, imported.EligibleVoterCount);
        Assert.Equal(2, await database.Context.ElectionResults.CountAsync());
        Assert.Contains(await database.Context.ElectionResults.ToListAsync(), item => item.VoteCount == 0);
        Assert.Equal(0, await database.Context.AnonymousBallots.CountAsync());
        Assert.Equal(0, await database.Context.ParticipationRecords.CountAsync());
        var transition = await database.Context.ElectionTransitions.SingleAsync();
        Assert.Equal((ElectionStatus.Finalized, ElectionStatus.Archived), (transition.FromStatus, transition.ToStatus));

        var record = await new PublicElectionService(new ElectionStore(database.Context), new TestHashService())
            .RecordAsync(input.Slug, default);
        Assert.Equal(new TurnoutView(120, 100, 90, 2, 83.33m), record.Turnout);
        Assert.Equal(2, record.Results.Count);
    }

    [Fact]
    public async Task ImportHistorical_RejectsBallotCountsExceedingParticipation()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var service = CreateService(database.Context);
        var input = HistoricalPartyElection(
            participatingVoterCount: 10,
            invalidBallotCount: 2,
            partyLists: [new(null, "HIST-1", "Historical Party", "List", 1, null, 9)]);

        await Assert.ThrowsAsync<ElectionValidationException>(() => service.ImportHistoricalAsync(input, default));

        Assert.Equal(0, await database.Context.Elections.CountAsync());
    }

    private static HistoricalElectionInput HistoricalPartyElection(
        int participatingVoterCount,
        int invalidBallotCount,
        IReadOnlyList<HistoricalPartyListInput> partyLists) => new(
            $"historical-{Guid.NewGuid():N}",
            "Historical election",
            "Imported official record",
            ElectionType.PartyList,
            new DateTime(1998, 6, 19, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(1998, 6, 20, 14, 0, 0, DateTimeKind.Utc),
            "CZ",
            "National archive, volume 42",
            120,
            participatingVoterCount,
            invalidBallotCount,
            partyLists,
            null);

    private static AdminElectionService CreateService(ElectionDbContext context) => new(
        new ElectionStore(context),
        new TestHashService(),
        new NullOrganizationClient(),
        new NullPersonClient(),
        new TestActor());

    private sealed class TestActor : ICurrentActor
    {
        public Guid PersonId { get; } = Guid.NewGuid();
        public IReadOnlyList<string> Roles { get; } = ["election-service:admin"];
        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private sealed class TestHashService : ICredentialHashService
    {
        public string ActiveKeyVersion => "test-v1";
        public string HashCitizen(Guid electionId, Guid personId, string keyVersion) => "citizen-hash";
        public (string Token, string Hash) CreateInvitation(Guid electionId, string keyVersion) => ("one-time-token", "stored-hash");
        public string HashInvitation(Guid electionId, string token, string keyVersion) => "stored-hash";
    }

    private sealed class NullOrganizationClient : IOrganizationRegistryClient
    {
        public Task<OrganizationSnapshot?> GetAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<OrganizationSnapshot?>(null);
    }

    private sealed class NullPersonClient : IPersonRegistryClient
    {
        public Task<PersonSnapshot?> GetAsync(Guid personId, CancellationToken ct) => Task.FromResult<PersonSnapshot?>(null);
    }
}