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
    public async Task AddPartyListAndCandidate_AllowsIndependentIdentities()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: ElectionStatus.Draft);
        var service = CreateService(database.Context);

        var partyList = await service.AddPartyListAsync(
            election.Id,
            new PartyListInput(null, "Independent Civic List", null, "Central list", 2),
            default);
        var candidate = await service.AddCandidateAsync(
            election.Id,
            partyList.Id,
            new CandidateInput(null, "Alex Example", "Independent candidate", 1),
            default);

        Assert.Null(partyList.PartyOrganizationId);
        Assert.Equal("Independent Civic List", partyList.PartyName);
        Assert.Equal("Alex Example", candidate.DisplayName);
        Assert.Null((await database.Context.Candidates.SingleAsync(item => item.Id == candidate.Id)).PersonId);
    }

    [Fact]
    public async Task AddPartyList_RejectsEmptyOrganizationIdExplicitly()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: ElectionStatus.Draft);
        var service = CreateService(database.Context);

        var exception = await Assert.ThrowsAsync<ElectionValidationException>(() =>
            service.AddPartyListAsync(
                election.Id,
                new PartyListInput(Guid.Empty, null, null, "Invalid list", 2),
                default));

        Assert.Equal("Party organization ID cannot be empty.", exception.Message);
    }

    [Fact]
    public async Task SetWinners_EnforcesSeatCountAndPersistsSelectedCandidates()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, partyList) = await database.SeedPartyElectionAsync(
            DateTime.UtcNow, status: ElectionStatus.Finalized);
        election.SeatCount = 1;
        var first = Candidate(partyList.Id, "First candidate", 1);
        var second = Candidate(partyList.Id, "Second candidate", 2);
        database.Context.Candidates.AddRange(first, second);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        await Assert.ThrowsAsync<ElectionValidationException>(() =>
            service.SetWinnersAsync(election.Id, new WinnerSelectionInput([first.Id, second.Id]), default));

        var updated = await service.SetWinnersAsync(
            election.Id, new WinnerSelectionInput([second.Id]), default);

        Assert.Equal(1, updated.SeatCount);
        Assert.False(updated.PartyLists.Single().Candidates.Single(item => item.Id == first.Id).IsWinner);
        Assert.True(updated.PartyLists.Single().Candidates.Single(item => item.Id == second.Id).IsWinner);
        var persisted = await database.Context.Candidates.AsNoTracking().SingleAsync(item => item.Id == second.Id);
        Assert.True(persisted.IsWinner);
        Assert.NotNull(persisted.WinnerSelectedAt);
        Assert.NotNull(persisted.WinnerSelectedByPersonId);
    }

    [Fact]
    public async Task TabularResults_ReturnsLivePartyTotalsAndCandidateLists()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, firstParty) = await database.SeedPartyElectionAsync(
            DateTime.UtcNow, status: ElectionStatus.Published);
        var secondParty = new PartyList
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, PartyOrganizationId = Guid.NewGuid(),
            PartyRegistrationNumber = "REG-2", PartyName = "Second Party", ListName = "Second List", SortOrder = 2
        };
        database.Context.PartyLists.Add(secondParty);
        database.Context.Candidates.Add(Candidate(firstParty.Id, "Listed candidate", 1));
        database.Context.AnonymousBallots.AddRange(
            Ballot(election.Id, firstParty.Id), Ballot(election.Id, firstParty.Id));
        await database.Context.SaveChangesAsync();

        var results = await new PublicElectionService(new ElectionStore(database.Context), new TestHashService())
            .TabularResultsAsync(election.Id, default);

        Assert.True(results.IsLive);
        Assert.Equal(2, results.TotalValidBallots);
        Assert.Equal(2, results.PartyGroups.Count);
        Assert.Equal(2, results.PartyGroups.Single(item => item.PartyListId == firstParty.Id).VoteCount);
        Assert.Equal(0, results.PartyGroups.Single(item => item.PartyListId == secondParty.Id).VoteCount);
        Assert.Single(results.PartyGroups.Single(item => item.PartyListId == firstParty.Id).Candidates);
    }

    [Theory]
    [InlineData(ElectionStatus.Draft)]
    [InlineData(ElectionStatus.Published)]
    [InlineData(ElectionStatus.Closed)]
    [InlineData(ElectionStatus.Finalized)]
    [InlineData(ElectionStatus.Certified)]
    [InlineData(ElectionStatus.Archived)]
    public async Task TabularResults_ReturnsTwoPartyFiveSeatElectionInEveryState(ElectionStatus status)
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow, status: status, seatCount: 5);
        database.Context.PartyLists.Add(new PartyList
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, PartyOrganizationId = Guid.NewGuid(),
            PartyRegistrationNumber = "REG-2", PartyName = "Second Party", ListName = "Second List", SortOrder = 2
        });
        await database.Context.SaveChangesAsync();
        var service = new PublicElectionService(new ElectionStore(database.Context), new TestHashService());

        var visibleElection = await service.GetAsync(election.Id.ToString(), default);
        var results = await service.TabularResultsAsync(election.Id, default);

        Assert.Equal(status, visibleElection.Status);
        Assert.Equal(status.ToString(), results.Status);
        Assert.Equal(5, results.SeatCount);
        Assert.Equal(2, results.PartyGroups.Count);
        Assert.Equal(status == ElectionStatus.Published, results.IsLive);
    }

    [Fact]
    public async Task SetVisibility_HidesElectionFromAllPublicSurfacesButKeepsAdminAccess()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(
            DateTime.UtcNow, status: ElectionStatus.Published);
        var admin = CreateService(database.Context);
        var publicService = new PublicElectionService(new ElectionStore(database.Context), new TestHashService());

        var hidden = await admin.SetVisibilityAsync(
            election.Id, new ElectionVisibilityInput(false), default);

        Assert.False(hidden.IsPubliclyVisible);
        Assert.DoesNotContain(await publicService.ListAsync(default), item => item.Id == election.Id);
        await Assert.ThrowsAsync<ElectionNotFoundException>(() => publicService.GetAsync(election.Slug, default));
        await Assert.ThrowsAsync<ElectionNotFoundException>(() => publicService.TabularResultsAsync(election.Id, default));
        Assert.Contains(await admin.ListAsync(default), item => item.Id == election.Id && !item.IsPubliclyVisible);
    }

    [Fact]
    public async Task TransitionToCertified_RequiresEveryConfiguredSeatToHaveAWinner()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, partyList) = await database.SeedPartyElectionAsync(
            DateTime.UtcNow, status: ElectionStatus.Finalized);
        election.SeatCount = 1;
        var candidate = Candidate(partyList.Id, "Winning candidate", 1);
        database.Context.Candidates.Add(candidate);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        await Assert.ThrowsAsync<ElectionValidationException>(() => service.TransitionAsync(
            election.Id, new TransitionInput(ElectionStatus.Certified, null), default));

        await service.SetWinnersAsync(election.Id, new WinnerSelectionInput([candidate.Id]), default);
        var certified = await service.TransitionAsync(
            election.Id, new TransitionInput(ElectionStatus.Certified, null), default);

        Assert.Equal(ElectionStatus.Certified, certified.Status);
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

    private static Candidate Candidate(Guid partyListId, string displayName, int position) => new()
    {
        Id = Guid.NewGuid(), PartyListId = partyListId, DisplayName = displayName, Position = position
    };

    private static AnonymousBallot Ballot(Guid electionId, Guid selectionId) => new()
    {
        Id = Guid.NewGuid(), ElectionId = electionId, SelectionType = SelectionType.PartyList,
        SelectionId = selectionId, ReceiptHash = Guid.NewGuid().ToString("N")
    };

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