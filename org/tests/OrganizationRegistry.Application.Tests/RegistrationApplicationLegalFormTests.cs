using Egov.Platform.Identity;
using Moq;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Tests;

public sealed class RegistrationApplicationLegalFormTests
{
    [Fact]
    public async Task CreateDraftAsync_PersistsNormalizedActiveLegalForm()
    {
        RegistrationApplication? saved = null;
        var store = new Mock<IOrganizationRegistryStore>();
        store.Setup(value => value.GetLegalFormAsync("LTD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LegalFormDefinition { Code = "LTD", LabelEn = "Limited company", LabelCs = "Společnost" });
        store.Setup(value => value.AddApplicationAsync(It.IsAny<RegistrationApplication>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrationApplication, CancellationToken>((application, _) => saved = application)
            .Returns(Task.CompletedTask);
        store.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await CreateService(store.Object).CreateDraftAsync(CreateInput(" ltd "), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal("LTD", saved.LegalFormCode);
    }

    [Fact]
    public async Task CreateDraftAsync_RejectsInactiveLegalForm()
    {
        var store = new Mock<IOrganizationRegistryStore>();
        store.Setup(value => value.GetLegalFormAsync("LTD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LegalFormDefinition
            {
                Code = "LTD",
                LabelEn = "Limited company",
                LabelCs = "Společnost",
                IsActive = false
            });

        await Assert.ThrowsAsync<RegistryValidationException>(() =>
            CreateService(store.Object).CreateDraftAsync(CreateInput("LTD"), CancellationToken.None));
    }

    private static RegistrationApplicationService CreateService(IOrganizationRegistryStore store)
    {
        var actor = new Mock<ICurrentActor>();
        actor.SetupGet(value => value.PersonId).Returns(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        return new RegistrationApplicationService(store, new Mock<IRegistrationNumberGenerator>().Object, actor.Object);
    }

    private static CreateRegistrationInput CreateInput(string legalFormCode) =>
        new("Example Works", null, legalFormCode, "Manufacturing", "1 Registry Road", []);
}