using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using Egov.Platform.Documents;
using FluentAssertions;
using Moq;

namespace CitizenService.Application.Tests.Services;

public class DecisionDocumentServiceTests
{
    private readonly Mock<IApplicationRepository> _applications = new();
    private readonly Mock<IPersonClient> _people = new();
    private readonly CapturingRenderer _renderer = new();

    [Fact]
    public async Task GenerateAsync_BuildsFinalDecisionWithChronologicalNamedAuditTrail()
    {
        var applicationId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var application = new CitizenshipApplication
        {
            Id = applicationId,
            PersonId = applicantId,
            Status = ApplicationStatus.Approved,
            FormName = "citizenship_application",
            FormVersion = 3,
            CreatedAt = new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
            CreatedByPersonId = applicantId,
            SubmittedAt = new DateTime(2026, 7, 19, 8, 30, 0, DateTimeKind.Utc),
            ReviewedAt = new DateTime(2026, 7, 20, 14, 15, 0, DateTimeKind.Utc),
            DecisionReason = "All statutory requirements were met.",
            ReviewerPersonId = reviewerId
        };
        _applications.Setup(repository => repository.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _applications.Setup(repository => repository.GetTransitionsAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Transition(applicationId, reviewerId, ApplicationStatus.UnderReview, ApplicationStatus.Approved, new DateTime(2026, 7, 20, 14, 15, 0, DateTimeKind.Utc), application.DecisionReason),
                Transition(applicationId, applicantId, ApplicationStatus.Draft, ApplicationStatus.Submitted, new DateTime(2026, 7, 19, 8, 30, 0, DateTimeKind.Utc)),
                Transition(applicationId, reviewerId, ApplicationStatus.Submitted, ApplicationStatus.UnderReview, new DateTime(2026, 7, 20, 9, 0, 0, DateTimeKind.Utc))
            ]);
        _people.Setup(client => client.GetPersonAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDto { Id = applicantId, DisplayName = "Alex Applicant" });
        _people.Setup(client => client.GetPersonAsync(reviewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDto { Id = reviewerId, DisplayName = "Rhea Reviewer" });

        var result = await CreateService().GenerateAsync(applicationId, CancellationToken.None);

        result.Content.Should().Equal([1, 2, 3]);
        _renderer.Document.Should().NotBeNull();
        _renderer.Document!.Fields.Should().Contain(field => field.Label == "Applicant" && field.Value == "Alex Applicant");
        _renderer.Document.Fields.Should().Contain(field => field.Label == "Decision" && field.Value == "Approved");
        _renderer.Document.Fields.Should().Contain(field => field.Label == "Decision reason" && field.Value == application.DecisionReason);
        _renderer.Document.Signature.Should().Be(new OfficialDocumentSignature("Rhea Reviewer", "Reviewing officer"));
        _renderer.Document.AuditTrail.Select(entry => entry.ActorName)
            .Should().Equal("Alex Applicant", "Alex Applicant", "Rhea Reviewer", "Rhea Reviewer");
        _renderer.Document.AuditTrail.First().Action.Should().Be("Application created");
        _renderer.Document.AuditTrail.Select(entry => entry.OccurredAt)
            .Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.UnderReview)]
    public async Task GenerateAsync_RejectsApplicationWithoutFinalDecision(ApplicationStatus status)
    {
        var applicationId = Guid.NewGuid();
        _applications.Setup(repository => repository.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CitizenshipApplication { Id = applicationId, Status = status });

        var action = () => CreateService().GenerateAsync(applicationId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*approval or rejection*");
        _renderer.Document.Should().BeNull();
    }

    [Fact]
    public void PdfRenderer_ProducesPdfDocument()
    {
        var renderer = new PdfSharpOfficialDocumentRenderer();
        var document = new OfficialDocument(
            "MKLU-CIT-TEST",
            "Citizenship Application Decision",
            "United States of Mekniy and Lurk · Test application",
            new DateTimeOffset(2026, 7, 20, 14, 15, 0, TimeSpan.Zero),
            [
                new("Applicant", "Alex Applicant"),
                new("Decision", "Approved"),
                new("Decision reason", "All statutory requirements were met.")
            ],
            [new(new DateTimeOffset(2026, 7, 20, 14, 15, 0, TimeSpan.Zero), "Rhea Reviewer", "Under review to Approved", "Accepted")],
            new("Rhea Reviewer", "Reviewing officer"));

        var result = renderer.Render(document);

        result.Content.Should().StartWith([0x25, 0x50, 0x44, 0x46]);
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
    }

    private DecisionDocumentService CreateService()
        => new(_applications.Object, _people.Object, _renderer);

    private static ApplicationTransition Transition(
        Guid applicationId,
        Guid actorId,
        ApplicationStatus from,
        ApplicationStatus to,
        DateTime changedAt,
        string? reason = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            ChangedByPersonId = actorId,
            FromStatus = from,
            ToStatus = to,
            ChangedAt = changedAt,
            Reason = reason
        };

    private sealed class CapturingRenderer : IOfficialDocumentRenderer
    {
        public OfficialDocument? Document { get; private set; }

        public GeneratedDocument Render(OfficialDocument document)
        {
            Document = document;
            return new GeneratedDocument([1, 2, 3], "application/pdf", "decision.pdf");
        }
    }
}