using System.Text.Json;
using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace CitizenService.Application.Tests.Services;

public class ApplicationApprovalTests
{
    private readonly Guid _personId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();
    private readonly Mock<IApplicationRepository> _applications = new();
    private readonly Mock<IFormRepository> _forms = new();
    private readonly Mock<ICitizenRepository> _citizens = new();
    private readonly Mock<IRegistryFieldRepository> _registry = new();
    private readonly Mock<IPersonClient> _persons = new();
    private readonly Mock<ICurrentActor> _actor = new();
    private readonly Mock<ICitizenNumberGenerator> _citizenNumbers = new();

    [Fact]
    public async Task ApproveAsync_CreatesCitizenAndCopiesMatchingAnswers()
    {
        var application = CreateApplication("""{"date_of_birth":"2000-01-02","motivation":"Hello"}""");
        var field = CreateRequiredDateField();
        Citizen? createdCitizen = null;
        CitizenFieldValue? savedValue = null;

        ConfigureCommon(application, field);
        _citizens.Setup(repository => repository.GetByPersonIdAsync(_personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdCitizen);
        _citizenNumbers.Setup(generator => generator.Generate())
            .Returns("CIT-7A3F-19C2-8D44-BE01-62AF-90D3");
        _citizens.Setup(repository => repository.AddAsync(It.IsAny<Citizen>(), It.IsAny<CancellationToken>()))
            .Callback<Citizen, CancellationToken>((citizen, _) => createdCitizen = citizen)
            .ReturnsAsync((Citizen citizen, CancellationToken _) => citizen);
        _registry.Setup(repository => repository.GetDefinitionByKeyAsync(field.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(field);
        _registry.Setup(repository => repository.GetValueAsync(It.IsAny<Guid>(), field.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitizenFieldValue?)null);
        _registry.Setup(repository => repository.SaveValueAsync(It.IsAny<CitizenFieldValue>(), It.IsAny<CancellationToken>()))
            .Callback<CitizenFieldValue, CancellationToken>((value, _) => savedValue = value)
            .ReturnsAsync((CitizenFieldValue value, CancellationToken _) => value);

        var service = CreateService();
        var result = await service.TransitionAsync(
            application.Id, ApplicationStatus.Approved, "Accepted", CancellationToken.None);

        result.Status.Should().Be(ApplicationStatus.Approved);
        createdCitizen.Should().NotBeNull();
        createdCitizen!.PersonId.Should().Be(_personId);
        createdCitizen.CitizenNumber.Should().Be("CIT-7A3F-19C2-8D44-BE01-62AF-90D3");
        createdCitizen.ImportSource.Should().Be($"application:{application.Id}");
        savedValue.Should().NotBeNull();
        savedValue!.Value.Should().Be("2000-01-02");
        savedValue.SourceApplicationId.Should().Be(application.Id);
        _applications.Verify(repository => repository.AddTransitionAsync(
            It.Is<ApplicationTransition>(transition => transition.ToStatus == ApplicationStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_DoesNotRecordApprovalWhenRequiredAnswerIsMissing()
    {
        var application = CreateApplication("""{"motivation":"Hello"}""");
        var field = CreateRequiredDateField();
        var citizen = new Citizen { Id = Guid.NewGuid(), PersonId = _personId };

        ConfigureCommon(application, field);
        _citizens.Setup(repository => repository.GetByPersonIdAsync(_personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(citizen);
        _registry.Setup(repository => repository.GetValueAsync(citizen.Id, field.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitizenFieldValue?)null);

        var service = CreateService();
        var action = () => service.TransitionAsync(
            application.Id, ApplicationStatus.Approved, null, CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*date_of_birth*");
        application.Status.Should().Be(ApplicationStatus.UnderReview);
        _applications.Verify(repository => repository.AddTransitionAsync(
            It.IsAny<ApplicationTransition>(), It.IsAny<CancellationToken>()), Times.Never);
        _applications.Verify(repository => repository.UpdateAsync(
            It.IsAny<CitizenshipApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void ConfigureCommon(CitizenshipApplication application, RegistryFieldDefinition field)
    {
        _actor.SetupGet(current => current.PersonId).Returns(_reviewerId);
        _persons.Setup(client => client.PersonExistsAsync(_personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _applications.Setup(repository => repository.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _applications.Setup(repository => repository.AddTransitionAsync(
            It.IsAny<ApplicationTransition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _applications.Setup(repository => repository.UpdateAsync(application, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _forms.Setup(repository => repository.GetFormAsync(
            application.FormName, application.FormVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationForm
            {
                Name = application.FormName,
                Version = application.FormVersion,
                DefinitionJson = """{"fields":[{"name":"date_of_birth","type":"date","required":true},{"name":"motivation","type":"textarea","required":true}]}"""
            });
        _registry.Setup(repository => repository.ListDefinitionsAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([field]);
    }

    private ApplicationAppService CreateService()
    {
        var citizenService = new CitizenAppService(
            _citizens.Object, _actor.Object, _persons.Object, _citizenNumbers.Object);
        var registryService = new RegistryFieldService(
            _registry.Object, _citizens.Object, _applications.Object, _forms.Object, _actor.Object);
        return new ApplicationAppService(
            _applications.Object,
            _forms.Object,
            _actor.Object,
            _persons.Object,
            citizenService,
            registryService,
            new ImmediateUnitOfWork());
    }

    private CitizenshipApplication CreateApplication(string answers)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = _personId,
            Status = ApplicationStatus.UnderReview,
            FormName = "citizenship_application",
            FormVersion = 1,
            FormAnswers = JsonDocument.Parse(answers)
        };

    private static RegistryFieldDefinition CreateRequiredDateField()
        => new()
        {
            Id = Guid.NewGuid(),
            Key = "date_of_birth",
            LabelsJson = """{"en":"Date of birth"}""",
            FieldType = RegistryFieldType.Date,
            IsRequired = true,
            IsActive = true
        };

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct)
            => operation();
    }
}
