using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Tests;

public sealed class FinalizationTests
{
    [Fact]
    public async Task Finalize_AggregatesAnonymousBallotsAndMovesClosedElectionToFinalized()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now, status: ElectionStatus.Closed);
        var zeroVoteSelection = new PartyList
        {
            Id = Guid.NewGuid(),
            ElectionId = election.Id,
            PartyOrganizationId = Guid.NewGuid(),
            PartyRegistrationNumber = "REG-ZERO",
            PartyName = "Zero Vote Party",
            ListName = "Zero Vote List",
            SortOrder = 2
        };
        database.Context.PartyLists.Add(zeroVoteSelection);
        database.Context.AnonymousBallots.AddRange(
            Ballot(election.Id, selection.Id, election.TerritoryCode),
            Ballot(election.Id, selection.Id, election.TerritoryCode));
        await database.Context.SaveChangesAsync();
        var store = new ElectionStore(database.Context);

        await store.FinalizeAsync(election.Id, Guid.NewGuid(), "Counted", now, default);

        var results = await database.Context.ElectionResults.AsNoTracking().OrderBy(item => item.VoteCount).ToListAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal((zeroVoteSelection.Id, zeroVoteSelection.ListName, 0),
            (results[0].SelectionId, results[0].SelectionLabel, results[0].VoteCount));
        var result = results[1];
        var finalizedElection = await database.Context.Elections.AsNoTracking().SingleAsync();
        Assert.Equal((selection.Id, selection.ListName, 2), (result.SelectionId, result.SelectionLabel, result.VoteCount));
        Assert.Equal(ElectionStatus.Finalized, finalizedElection.Status);
        Assert.Equal(now, finalizedElection.FinalizedAt);
        Assert.Equal(2, await database.Context.AnonymousBallots.CountAsync());

        var resultEntity = database.Context.Model.FindEntityType(typeof(ElectionResult))!;
        Assert.DoesNotContain(resultEntity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(AnonymousBallot));
        Assert.DoesNotContain(resultEntity.GetNavigations(), navigation =>
            navigation.TargetEntityType.ClrType == typeof(AnonymousBallot));
    }

    [Fact]
    public async Task Finalize_CapturesSpecificVoterRollCountWhenEligibleCountIsAbsent()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, _) = await database.SeedPartyElectionAsync(
            now, EligibilityMode.SpecificVoterRoll, ElectionStatus.Closed);
        database.Context.VoterRollEntries.AddRange(
            Voter(election.Id),
            Voter(election.Id),
            Voter(election.Id));
        await database.Context.SaveChangesAsync();

        await new ElectionStore(database.Context).FinalizeAsync(election.Id, Guid.NewGuid(), null, now, default);

        Assert.Equal(3, (await database.Context.Elections.AsNoTracking().SingleAsync()).EligibleVoterCount);
    }

    private static AnonymousBallot Ballot(Guid electionId, Guid selectionId, string? territoryCode) => new()
    {
        Id = Guid.NewGuid(),
        ElectionId = electionId,
        SelectionType = SelectionType.PartyList,
        SelectionId = selectionId,
        TerritoryCode = territoryCode
    };

    private static VoterRollEntry Voter(Guid electionId) => new()
    {
        ElectionId = electionId,
        PersonId = Guid.NewGuid(),
        AddedAt = DateTime.UtcNow,
        AddedByPersonId = Guid.NewGuid()
    };
}