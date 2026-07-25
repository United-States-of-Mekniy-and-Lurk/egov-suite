using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ElectionService.Tests;

internal sealed class ElectionTestDatabase : IAsyncDisposable
{
    private ElectionTestDatabase(ElectionDbContext context)
    {
        Context = context;
    }

    public ElectionDbContext Context { get; }

    public static async Task<ElectionTestDatabase> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<ElectionDbContext>()
            .UseInMemoryDatabase($"election-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new ElectionDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new ElectionTestDatabase(context);
    }

    public async Task<(Election Election, PartyList Selection)> SeedPartyElectionAsync(
        DateTime now,
        EligibilityMode eligibilityMode = EligibilityMode.AllActiveCitizens,
        ElectionStatus status = ElectionStatus.Published)
    {
        var election = new Election
        {
            Id = Guid.NewGuid(),
            Slug = $"election-{Guid.NewGuid():N}",
            Title = "Test election",
            Description = "Privacy contract test election",
            Type = ElectionType.PartyList,
            Status = status,
            EligibilityMode = eligibilityMode,
            VotingStartsAt = now.AddHours(-1),
            VotingEndsAt = now.AddHours(1),
            TerritoryCode = "CZ-10",
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now,
            CreatedByPersonId = Guid.NewGuid()
        };
        var selection = new PartyList
        {
            Id = Guid.NewGuid(),
            ElectionId = election.Id,
            Election = election,
            PartyOrganizationId = Guid.NewGuid(),
            PartyRegistrationNumber = $"REG-{Guid.NewGuid():N}",
            PartyName = "Test Party",
            ListName = "Test List",
            SortOrder = 1
        };
        election.PartyLists.Add(selection);
        Context.Elections.Add(election);
        await Context.SaveChangesAsync();
        return (election, selection);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}