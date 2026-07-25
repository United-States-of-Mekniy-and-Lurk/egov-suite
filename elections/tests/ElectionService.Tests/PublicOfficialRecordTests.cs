using System.Reflection;
using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Application.Services;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Infrastructure.Persistence;

namespace ElectionService.Tests;

public sealed class PublicOfficialRecordTests
{
    [Fact]
    public async Task Record_LiveFinalizedElectionReturnsAggregateCountsOnly()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        var (election, selection) = await database.SeedPartyElectionAsync(now, status: ElectionStatus.Finalized);
        election.EligibleVoterCount = 4;
        database.Context.ParticipationRecords.AddRange(
            Participation(election.Id, "credential-a"),
            Participation(election.Id, "credential-b"));
        database.Context.AnonymousBallots.Add(new AnonymousBallot
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, SelectionType = SelectionType.PartyList,
            SelectionId = selection.Id, TerritoryCode = election.TerritoryCode
        });
        database.Context.ElectionResults.Add(new ElectionResult
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, SelectionType = SelectionType.PartyList,
            SelectionId = selection.Id, SelectionLabel = selection.ListName,
            TerritoryCode = election.TerritoryCode, VoteCount = 1, FinalizedAt = now
        });
        await database.Context.SaveChangesAsync();
        var service = new PublicElectionService(new ElectionStore(database.Context), new TestHashService());

        var record = await service.RecordAsync(election.Slug, default);

        Assert.Equal(new TurnoutView(4, 2, 1, 0, 50m), record.Turnout);
        Assert.Single(record.Results);
        var publicProperties = PublicPropertyNames<OfficialElectionRecordView>()
            .Concat(PublicPropertyNames<TurnoutView>())
            .Concat(PublicPropertyNames<ResultView>())
            .ToList();
        Assert.DoesNotContain(publicProperties, name =>
            name.Contains("Credential", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("VoterId", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Timestamp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Record_PublishedElectionIsNotAvailable()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var (election, _) = await database.SeedPartyElectionAsync(DateTime.UtcNow);
        var service = new PublicElectionService(new ElectionStore(database.Context), new TestHashService());

        await Assert.ThrowsAsync<ElectionNotFoundException>(() => service.RecordAsync(election.Slug, default));
    }

    private static ParticipationRecord Participation(Guid electionId, string credentialHash) => new()
    {
        Id = Guid.NewGuid(), ElectionId = electionId, Channel = ParticipationChannel.Citizen,
        CredentialHash = credentialHash, RecordedOn = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    private static string[] PublicPropertyNames<T>() => typeof(T)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Select(property => property.Name)
        .ToArray();

    private sealed class TestHashService : ICredentialHashService
    {
        public string ActiveKeyVersion => "test-v1";
        public string HashCitizen(Guid electionId, Guid personId, string keyVersion) => "citizen-hash";
        public (string Token, string Hash) CreateInvitation(Guid electionId, string keyVersion) => ("token", "hash");
        public string HashInvitation(Guid electionId, string token, string keyVersion) => "hash";
    }
}