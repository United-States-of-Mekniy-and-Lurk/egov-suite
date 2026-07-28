using System.Reflection;
using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Tests;

public sealed class PrivacyContractTests
{
    [Fact]
    public void AnonymousBallot_HasOnlyAnonymousSelectionProperties()
    {
        var propertyNames = PublicPropertyNames<AnonymousBallot>();

        Assert.Equal(
            ["ElectionId", "Id", "ReceiptHash", "SelectionId", "SelectionType", "TerritoryCode"],
            propertyNames.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void ParticipationAndInvitation_DoNotExposeBallotOrSelectionLinks()
    {
        Assert.DoesNotContain(PublicPropertyNames<ParticipationRecord>(), IsBallotLink);
        Assert.DoesNotContain(PublicPropertyNames<VotingInvitation>(), IsBallotLink);
        Assert.Empty(PublicNavigationProperties<ParticipationRecord>());
        Assert.Empty(PublicNavigationProperties<VotingInvitation>());
    }

    [Fact]
    public void InvitationAdminView_ExposesOnlySafeAdministrationFields()
    {
        Assert.Equal(
            ["CreatedAt", "CreatedByPersonId", "Id", "Label", "PersonId", "RevokedAt", "Token", "UsedOn"],
            PublicPropertyNames<InvitationAdminView>().OrderBy(name => name, StringComparer.Ordinal));
        Assert.DoesNotContain("TokenHash", PublicPropertyNames<InvitationAdminView>());
    }

    [Fact]
    public async Task RelationalModel_KeepsBallotsSeparateFromParticipationAndInvitations()
    {
        await using var database = await ElectionTestDatabase.CreateAsync();
        var model = database.Context.Model;
        var ballot = model.FindEntityType(typeof(AnonymousBallot))!;
        var foreignKey = Assert.Single(ballot.GetForeignKeys());

        Assert.Equal(nameof(AnonymousBallot.ElectionId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(typeof(Election), foreignKey.PrincipalEntityType.ClrType);
        Assert.Null(foreignKey.DependentToPrincipal);
        Assert.Null(foreignKey.PrincipalToDependent);

        var participation = model.FindEntityType(typeof(ParticipationRecord))!;
        var uniqueIndex = Assert.Single(participation.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(ParticipationRecord.ElectionId), nameof(ParticipationRecord.Channel), nameof(ParticipationRecord.CredentialHash)]));
        Assert.True(uniqueIndex.IsUnique);

        Assert.DoesNotContain(ballot.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(ParticipationRecord) ||
            key.PrincipalEntityType.ClrType == typeof(VotingInvitation));
    }

    private static string[] PublicPropertyNames<T>() => typeof(T)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Select(property => property.Name)
        .ToArray();

    private static PropertyInfo[] PublicNavigationProperties<T>() => typeof(T)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.PropertyType.IsClass && property.PropertyType != typeof(string))
        .ToArray();

    private static bool IsBallotLink(string name) =>
        name.Contains("Ballot", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Selection", StringComparison.OrdinalIgnoreCase);
}