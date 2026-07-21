using CitizenService.Application.Documents;
using CitizenService.Application.Interfaces;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Services;

public sealed class DecisionDocumentService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IPersonClient _personClient;
    private readonly IOfficialDocumentRenderer _renderer;

    public DecisionDocumentService(
        IApplicationRepository applicationRepository,
        IPersonClient personClient,
        IOfficialDocumentRenderer renderer)
    {
        _applicationRepository = applicationRepository;
        _personClient = personClient;
        _renderer = renderer;
    }

    public async Task<GeneratedDocument> GenerateAsync(Guid applicationId, CancellationToken ct)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} was not found.");
        if (application.Status is not (ApplicationStatus.Approved or ApplicationStatus.Rejected))
            throw new InvalidOperationException("A decision document is only available after approval or rejection.");
        if (!application.ReviewerPersonId.HasValue || !application.ReviewedAt.HasValue)
            throw new InvalidOperationException("The application decision record is incomplete.");

        var transitions = (await _applicationRepository.GetTransitionsAsync(applicationId, ct))
            .OrderBy(transition => transition.ChangedAt)
            .ToList();
        var actorIds = transitions
            .Select(transition => transition.ChangedByPersonId)
            .Append(application.PersonId)
            .Append(application.ReviewerPersonId.Value)
            .Append(application.CreatedByPersonId)
            .Where(personId => personId != Guid.Empty)
            .Distinct()
            .ToList();
        var people = new Dictionary<Guid, PersonDto?>();
        foreach (var actorId in actorIds)
            people[actorId] = await _personClient.GetPersonAsync(actorId, ct);

        var applicantName = GetDisplayName(people, application.PersonId);
        var reviewerName = GetDisplayName(people, application.ReviewerPersonId.Value);
        var decisionLabel = application.Status == ApplicationStatus.Approved ? "Approved" : "Rejected";
        var documentNumber = $"MKLU-CIT-{application.Id:N}".ToUpperInvariant();
        var issuedAt = new DateTimeOffset(DateTime.SpecifyKind(application.ReviewedAt.Value, DateTimeKind.Utc));
        var auditTrail = new List<OfficialDocumentAuditEntry>
        {
            new(
                new DateTimeOffset(DateTime.SpecifyKind(application.CreatedAt, DateTimeKind.Utc)),
                application.CreatedByPersonId == Guid.Empty
                    ? "System"
                    : GetDisplayName(people, application.CreatedByPersonId),
                "Application created",
                null)
        };
        auditTrail.AddRange(transitions.Select(transition => new OfficialDocumentAuditEntry(
            new DateTimeOffset(DateTime.SpecifyKind(transition.ChangedAt, DateTimeKind.Utc)),
            GetDisplayName(people, transition.ChangedByPersonId),
            $"{FormatStatus(transition.FromStatus)} to {FormatStatus(transition.ToStatus)}",
            transition.Reason)));

        var document = new OfficialDocument(
            documentNumber,
            "Citizenship Application Decision",
            $"United States of Mekniy and Lurk · Application {application.Id}",
            issuedAt,
            [
                new("Applicant", applicantName),
                new("Applicant person ID", application.PersonId.ToString()),
                new("Decision", decisionLabel),
                new("Decision reason", application.DecisionReason ?? "No reason was recorded."),
                new("Submitted", FormatTimestamp(application.SubmittedAt)),
                new("Decided", FormatTimestamp(application.ReviewedAt)),
                new("Application form", $"{application.FormName} version {application.FormVersion}")
            ],
            auditTrail,
            new OfficialDocumentSignature(reviewerName, "Reviewing officer"));

        return _renderer.Render(document);
    }

    private static string GetDisplayName(IReadOnlyDictionary<Guid, PersonDto?> people, Guid personId)
    {
        if (people.TryGetValue(personId, out var person) && person != null)
        {
            if (!string.IsNullOrWhiteSpace(person.DisplayName)) return person.DisplayName;
            if (!string.IsNullOrWhiteSpace(person.PreferredUsername)) return person.PreferredUsername;
        }

        return personId.ToString();
    }

    private static string FormatStatus(ApplicationStatus status)
        => status == ApplicationStatus.UnderReview ? "Under review" : status.ToString();

    private static string FormatTimestamp(DateTime? timestamp)
        => timestamp.HasValue
            ? DateTime.SpecifyKind(timestamp.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm 'UTC'")
            : "Not recorded";
}