using Egov.Platform.Identity;
using Moq;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Tests;

public sealed class HistoricalOrganizationServiceTests
{
    private static readonly Guid ClerkId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OwnerId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task CreateAsync_PreservesHistoricalDatesAndUsesCurrentAuditTimestamps()
    {
        var classification = new ClassificationDefinition
        {
            Id = Guid.NewGuid(),
            Scheme = "organization-category",
            Code = "business",
            LabelEn = "Business",
            LabelCs = "Podnik"
        };
        Organization? savedOrganization = null;
        OrganizationAccessGrant? savedGrant = null;
        var store = new Mock<IOrganizationRegistryStore>();
        store.Setup(value => value.RegistrationNumberExistsAsync("LEGACY-0042", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        store.Setup(value => value.SlugExistsAsync("legacy-works", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        store.Setup(value => value.GetClassificationsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([classification]);
        store.Setup(value => value.AddOrganizationAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((organization, _) => savedOrganization = organization)
            .Returns(Task.CompletedTask);
        store.Setup(value => value.AddAccessGrantAsync(It.IsAny<OrganizationAccessGrant>(), It.IsAny<CancellationToken>()))
            .Callback<OrganizationAccessGrant, CancellationToken>((grant, _) => savedGrant = grant)
            .Returns(Task.CompletedTask);
        store.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var before = DateTime.UtcNow;

        var result = await new HistoricalOrganizationService(store.Object, CreateActor(true)).CreateAsync(
            CreateInput(),
            CancellationToken.None);

        var after = DateTime.UtcNow;
        Assert.NotNull(savedOrganization);
        Assert.Equal(new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc), savedOrganization.RegisteredAt);
        Assert.Equal(new DateOnly(1997, 11, 3), savedOrganization.EstablishedOn);
        Assert.Equal("Archive volume 12, folio 48", savedOrganization.ImportSourceReference);
        Assert.InRange(savedOrganization.CreatedAt, before, after);
        Assert.Equal(savedOrganization.CreatedAt, savedOrganization.UpdatedAt);
        Assert.Equal(ClerkId, savedOrganization.CreatedByPersonId);
        Assert.Single(savedOrganization.Classifications);
        Assert.InRange(savedOrganization.Classifications[0].AssignedAt, before, after);
        Assert.NotNull(savedGrant);
        Assert.Equal(OwnerId, savedGrant.PersonId);
        Assert.InRange(savedGrant.ValidFrom, before, after);
        Assert.Equal(savedOrganization.RegisteredAt, result.RegisteredAt);
        Assert.Equal(savedOrganization.EstablishedOn, result.EstablishedOn);
        store.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsNonClerk()
    {
        var store = new Mock<IOrganizationRegistryStore>();
        var service = new HistoricalOrganizationService(store.Object, CreateActor(false));

        await Assert.ThrowsAsync<RegistryForbiddenException>(() => service.CreateAsync(CreateInput(), CancellationToken.None));

        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_RejectsEstablishmentAfterRegistration()
    {
        var service = new HistoricalOrganizationService(new Mock<IOrganizationRegistryStore>().Object, CreateActor(true));
        var input = CreateInput() with { EstablishedOn = new DateOnly(1999, 1, 1) };

        await Assert.ThrowsAsync<RegistryValidationException>(() => service.CreateAsync(input, CancellationToken.None));
    }

    private static CreateHistoricalOrganizationInput CreateInput() => new(
        "LEGACY-0042",
        new DateOnly(1998, 4, 12),
        new DateOnly(1997, 11, 3),
        "Archive volume 12, folio 48",
        "Imported from the paper register.",
        OwnerId,
        "Legacy Works",
        null,
        "COOP",
        "Fabrication and repair",
        "42 Archive Road",
        ["business"]);

    private static ICurrentActor CreateActor(bool isClerk)
    {
        var actor = new Mock<ICurrentActor>();
        actor.SetupGet(value => value.PersonId).Returns(ClerkId);
        actor.Setup(value => value.IsInRole("organization-registry:clerk")).Returns(isClerk);
        actor.Setup(value => value.IsInRole("organization-registry:admin")).Returns(false);
        return actor.Object;
    }
}