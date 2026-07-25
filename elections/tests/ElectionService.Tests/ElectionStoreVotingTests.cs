using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Tests;

public sealed class ElectionStoreVotingTests
{
    [Fact]
    public async Task SubmitCitizenBallot_CreatesSeparateAnonymousBallotAndParticipationRecord()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now);
        var store = new ElectionStore(database.Context);

        await store.SubmitBallotAsync(CitizenCommand(election.Id, selection.Id, "citizen-hash", Guid.NewGuid(), now), now, default);

        var ballot = Assert.Single(await database.Context.AnonymousBallots.AsNoTracking().ToListAsync());
        var participation = Assert.Single(await database.Context.ParticipationRecords.AsNoTracking().ToListAsync());
        Assert.Equal((election.Id, selection.Id, election.TerritoryCode), (ballot.ElectionId, ballot.SelectionId, ballot.TerritoryCode));
        Assert.Equal((election.Id, ParticipationChannel.Citizen, "citizen-hash"),
            (participation.ElectionId, participation.Channel, participation.CredentialHash));
        Assert.NotEqual(ballot.Id, participation.Id);
    }

    [Fact]
    public async Task SubmitCitizenBallot_RejectsDuplicateCredentialWithoutAddingBallot()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now);
        var store = new ElectionStore(database.Context);
        var command = CitizenCommand(election.Id, selection.Id, "same-credential", Guid.NewGuid(), now);

        await store.SubmitBallotAsync(command, now, default);
        await Assert.ThrowsAsync<ElectionConflictException>(() => store.SubmitBallotAsync(command, now, default));

        Assert.Equal(1, await database.Context.AnonymousBallots.CountAsync());
        Assert.Equal(1, await database.Context.ParticipationRecords.CountAsync());
    }

    [Fact]
    public async Task SubmitInvitationBallot_MarksInvitationUsedAndRejectsSecondUse()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var recordedOn = DateOnly.FromDateTime(now);
        var (election, selection) = await database.SeedPartyElectionAsync(now);
        var invitation = new VotingInvitation
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, TokenHash = "invitation-hash",
            CreatedAt = now.AddDays(-1), CreatedByPersonId = Guid.NewGuid()
        };
        database.Context.VotingInvitations.Add(invitation);
        await database.Context.SaveChangesAsync();
        var store = new ElectionStore(database.Context);
        var command = new SubmitBallotCommand(election.Id, ParticipationChannel.Invitation,
            invitation.TokenHash, selection.Id, recordedOn, invitation.Id, null);

        await store.SubmitBallotAsync(command, now, default);
        await Assert.ThrowsAsync<ElectionConflictException>(() => store.SubmitBallotAsync(command, now, default));

        Assert.Equal(recordedOn, (await database.Context.VotingInvitations.SingleAsync()).UsedOn);
        Assert.Equal(1, await database.Context.AnonymousBallots.CountAsync());
        Assert.Equal(1, await database.Context.ParticipationRecords.CountAsync());
    }

    [Fact]
    public async Task SubmitCitizenBallot_EnforcesSpecificVoterRollInStoreTransaction()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var (election, selection) = await database.SeedPartyElectionAsync(now, EligibilityMode.SpecificVoterRoll);
        var store = new ElectionStore(database.Context);
        var command = CitizenCommand(election.Id, selection.Id, "eligible-hash", personId, now);

        await Assert.ThrowsAsync<ElectionForbiddenException>(() => store.SubmitBallotAsync(command, now, default));
        database.Context.VoterRollEntries.Add(new VoterRollEntry { ElectionId = election.Id, PersonId = personId });
        await database.Context.SaveChangesAsync();
        await store.SubmitBallotAsync(command, now, default);

        Assert.Equal(1, await database.Context.AnonymousBallots.CountAsync());
    }

    [Fact]
    public async Task SubmitCitizenBallot_AllCitizenModeDoesNotRequireVoterRollEntry()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now, EligibilityMode.AllActiveCitizens);
        var store = new ElectionStore(database.Context);

        await store.SubmitBallotAsync(CitizenCommand(election.Id, selection.Id, "all-citizen-hash", Guid.NewGuid(), now), now, default);

        Assert.Equal(1, await database.Context.AnonymousBallots.CountAsync());
    }

    [Theory]
    [InlineData(ElectionStatus.Closed, 0)]
    [InlineData(ElectionStatus.Published, 2)]
    public async Task SubmitCitizenBallot_RejectsClosedOrOutOfWindowElection(ElectionStatus status, int hoursAfterEnd)
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now, status: status);
        var submittedAt = now.AddHours(hoursAfterEnd);
        var store = new ElectionStore(database.Context);

        await Assert.ThrowsAsync<ElectionValidationException>(() => store.SubmitBallotAsync(
            CitizenCommand(election.Id, selection.Id, $"window-{status}", Guid.NewGuid(), submittedAt), submittedAt, default));

        Assert.Empty(database.Context.AnonymousBallots);
    }

    [Fact]
    public async Task SubmitCitizenBallot_RejectsSelectionFromAnotherElection()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, _) = await database.SeedPartyElectionAsync(now);
        var (_, otherSelection) = await database.SeedPartyElectionAsync(now);
        var store = new ElectionStore(database.Context);

        await Assert.ThrowsAsync<ElectionValidationException>(() => store.SubmitBallotAsync(
            CitizenCommand(election.Id, otherSelection.Id, "wrong-selection", Guid.NewGuid(), now), now, default));

        Assert.Empty(database.Context.AnonymousBallots);
    }

    private static SubmitBallotCommand CitizenCommand(Guid electionId, Guid selectionId, string hash, Guid personId, DateTime now) =>
        new(electionId, ParticipationChannel.Citizen, hash, selectionId, DateOnly.FromDateTime(now), null, personId);
}