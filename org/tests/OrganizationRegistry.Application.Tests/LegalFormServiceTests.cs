using Egov.Platform.Identity;
using Moq;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Tests;

public sealed class LegalFormServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesCodeAndPersistsDefinition()
    {
        LegalFormDefinition? saved = null;
        var store = new Mock<IOrganizationRegistryStore>();
        store.Setup(value => value.GetLegalFormAsync("NON-PROFIT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalFormDefinition?)null);
        store.Setup(value => value.AddLegalFormAsync(It.IsAny<LegalFormDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<LegalFormDefinition, CancellationToken>((legalForm, _) => saved = legalForm)
            .Returns(Task.CompletedTask);
        store.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService(store.Object).CreateAsync(
            new CreateLegalFormInput(" non-profit ", "Non-profit", "Nezisková organizace", 60),
            CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("NON-PROFIT", saved.Code);
        Assert.Equal("NON-PROFIT", result.Code);
        Assert.True(result.IsActive);
        store.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidCode()
    {
        var service = CreateService(new Mock<IOrganizationRegistryStore>().Object);

        await Assert.ThrowsAsync<RegistryValidationException>(() => service.CreateAsync(
            new CreateLegalFormInput("limited company", "Limited company", "Společnost", 10),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateDefinitionWithoutChangingCode()
    {
        var legalForm = new LegalFormDefinition
        {
            Code = "LTD",
            LabelEn = "Limited company",
            LabelCs = "Společnost",
            IsActive = true,
            SortOrder = 10
        };
        var store = new Mock<IOrganizationRegistryStore>();
        store.Setup(value => value.GetLegalFormAsync("LTD", It.IsAny<CancellationToken>())).ReturnsAsync(legalForm);
        store.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService(store.Object).UpdateAsync(
            "LTD",
            new UpdateLegalFormInput("Limited liability company", "Společnost s ručením omezeným", false, 20),
            CancellationToken.None);

        Assert.Equal("LTD", result.Code);
        Assert.False(result.IsActive);
        Assert.Equal(20, result.SortOrder);
    }

    [Fact]
    public async Task CreateAsync_RejectsNonAdmin()
    {
        var actor = new Mock<ICurrentActor>();
        actor.SetupGet(value => value.PersonId).Returns(Guid.NewGuid());
        var service = new LegalFormService(new Mock<IOrganizationRegistryStore>().Object, actor.Object);

        await Assert.ThrowsAsync<RegistryForbiddenException>(() => service.CreateAsync(
            new CreateLegalFormInput("LTD", "Limited company", "Společnost", 10),
            CancellationToken.None));
    }

    private static LegalFormService CreateService(IOrganizationRegistryStore store)
    {
        var actor = new Mock<ICurrentActor>();
        actor.SetupGet(value => value.PersonId).Returns(Guid.Parse("40000000-0000-0000-0000-000000000004"));
        actor.Setup(value => value.IsInRole("organization-registry:admin")).Returns(true);
        return new LegalFormService(store, actor.Object);
    }
}