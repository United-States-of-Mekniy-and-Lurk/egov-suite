using CitizenService.Application.Interfaces;
using CitizenService.Application.Models;
using CitizenService.Application.Services;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using FluentAssertions;
using Moq;

namespace CitizenService.Application.Tests.Services;

public class FieldCorrectionServiceTests
{
    private readonly Guid _personId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();
    private readonly Citizen _citizen;
    private readonly RegistryFieldDefinition _definition;
    private readonly Mock<IFieldCorrectionRepository> _corrections = new();
    private readonly Mock<IRegistryFieldRepository> _registry = new();
    private readonly Mock<ICitizenRepository> _citizens = new();
    private readonly Mock<IApplicationRepository> _applications = new();
    private readonly Mock<IFormRepository> _forms = new();
    private readonly Mock<ICurrentActor> _actor = new();

    public FieldCorrectionServiceTests()
    {
        _citizen = new Citizen { Id = Guid.NewGuid(), PersonId = _personId };
        _definition = new RegistryFieldDefinition
        {
            Id = Guid.NewGuid(),
            Key = "legal_name",
            LabelsJson = """{"en":"Legal name"}""",
            FieldType = RegistryFieldType.Text,
            IsActive = true,
            UserEditable = true
        };
        _citizens.Setup(repository => repository.GetByPersonIdAsync(_personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_citizen);
        _citizens.Setup(repository => repository.GetByIdAsync(_citizen.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_citizen);
        _registry.Setup(repository => repository.GetDefinitionByKeyAsync(_definition.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_definition);
        _registry.Setup(repository => repository.GetDefinitionByIdAsync(_definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_definition);
        _actor.SetupGet(actor => actor.PersonId).Returns(_personId);
    }

    [Fact]
    public async Task SubmitAsync_SnapshotsCurrentValueAndNormalizesProposal()
    {
        var current = CurrentValue("Old name");
        FieldCorrectionRequest? saved = null;
        _registry.Setup(repository => repository.GetValueAsync(_citizen.Id, _definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        _corrections.Setup(repository => repository.GetPendingAsync(_citizen.Id, _definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FieldCorrectionRequest?)null);
        _corrections.Setup(repository => repository.AddAsync(It.IsAny<FieldCorrectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FieldCorrectionRequest, CancellationToken>((request, _) => saved = request)
            .ReturnsAsync((FieldCorrectionRequest request, CancellationToken _) => request);

        var result = await CreateService().SubmitAsync(
            _personId,
            _definition.Key,
            new SubmitFieldCorrectionInput("  New name  ", "The current spelling is wrong."),
            CancellationToken.None);

        result.Status.Should().Be(FieldCorrectionStatus.Submitted);
        result.CurrentValue.Should().Be("Old name");
        result.ProposedValue.Should().Be("New name");
        saved.Should().NotBeNull();
        saved!.RequestedByPersonId.Should().Be(_personId);
    }

    [Fact]
    public async Task SubmitAsync_RejectsSecondPendingRequestForField()
    {
        _corrections.Setup(repository => repository.GetPendingAsync(_citizen.Id, _definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FieldCorrectionRequest { Status = FieldCorrectionStatus.Submitted });

        var action = () => CreateService().SubmitAsync(
            _personId,
            _definition.Key,
            new SubmitFieldCorrectionInput("New name", "Correction"),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already awaiting review*");
        _corrections.Verify(repository => repository.AddAsync(
            It.IsAny<FieldCorrectionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_AppendsValueWithCorrectionProvenance()
    {
        var request = SubmittedRequest();
        CitizenFieldValue? replacement = null;
        _actor.SetupGet(actor => actor.PersonId).Returns(_reviewerId);
        _corrections.Setup(repository => repository.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _registry.Setup(repository => repository.GetValueAsync(_citizen.Id, _definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurrentValue(request.CurrentValue!));
        _registry.Setup(repository => repository.ReplaceCurrentValueAsync(
                It.IsAny<CitizenFieldValue?>(),
                It.IsAny<CitizenFieldValue>(),
                It.IsAny<CancellationToken>()))
            .Callback<CitizenFieldValue?, CitizenFieldValue, CancellationToken>((_, value, _) => replacement = value)
            .ReturnsAsync((CitizenFieldValue? _, CitizenFieldValue value, CancellationToken _) => value);
        _corrections.Setup(repository => repository.UpdateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await CreateService().ApproveAsync(
            request.Id,
            new ReviewFieldCorrectionInput("Evidence verified."),
            CancellationToken.None);

        result.Status.Should().Be(FieldCorrectionStatus.Approved);
        result.ReviewedByPersonId.Should().Be(_reviewerId);
        replacement.Should().NotBeNull();
        replacement!.Value.Should().Be(request.ProposedValue);
        replacement.SourceCorrectionRequestId.Should().Be(request.Id);
        replacement.ValidFrom.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RejectAsync_DoesNotChangeRegistryValue()
    {
        var request = SubmittedRequest();
        _actor.SetupGet(actor => actor.PersonId).Returns(_reviewerId);
        _corrections.Setup(repository => repository.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _corrections.Setup(repository => repository.UpdateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await CreateService().RejectAsync(
            request.Id,
            new ReviewFieldCorrectionInput("The supplied evidence was insufficient."),
            CancellationToken.None);

        result.Status.Should().Be(FieldCorrectionStatus.Rejected);
        _registry.Verify(repository => repository.ReplaceCurrentValueAsync(
            It.IsAny<CitizenFieldValue?>(),
            It.IsAny<CitizenFieldValue>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private FieldCorrectionService CreateService()
    {
        var registryService = new RegistryFieldService(
            _registry.Object,
            _citizens.Object,
            _applications.Object,
            _forms.Object,
            _actor.Object);
        return new FieldCorrectionService(
            _corrections.Object,
            _registry.Object,
            _citizens.Object,
            registryService,
            _actor.Object,
            new ImmediateUnitOfWork());
    }

    private CitizenFieldValue CurrentValue(string value)
        => new()
        {
            Id = Guid.NewGuid(),
            CitizenId = _citizen.Id,
            FieldDefinitionId = _definition.Id,
            Value = value,
            ValidFrom = DateTime.UtcNow.AddDays(-1)
        };

    private FieldCorrectionRequest SubmittedRequest()
        => new()
        {
            Id = Guid.NewGuid(),
            CitizenId = _citizen.Id,
            FieldDefinitionId = _definition.Id,
            RequestedByPersonId = _personId,
            CurrentValue = "Old name",
            ProposedValue = "New name",
            RequestReason = "The current spelling is wrong.",
            Status = FieldCorrectionStatus.Submitted,
            SubmittedAt = DateTime.UtcNow.AddHours(-1)
        };

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct)
            => operation();
    }
}