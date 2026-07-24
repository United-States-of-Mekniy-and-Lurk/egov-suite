using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Domain.Entities;

public sealed class RegistrationApplication
{
    public Guid Id { get; set; }
    public Guid ApplicantPersonId { get; set; }
    public RegistrationApplicationStatus Status { get; set; } = RegistrationApplicationStatus.Draft;
    public string LegalName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string LegalFormCode { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RegisteredAddress { get; set; } = string.Empty;
    public string[] RequestedClassificationCodes { get; set; } = [];
    public Guid? OrganizationId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewerPersonId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RegistrationTransition> Transitions { get; set; } = [];
}

public sealed class RegistrationTransition
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public RegistrationApplication Application { get; set; } = null!;
    public RegistrationApplicationStatus FromStatus { get; set; }
    public RegistrationApplicationStatus ToStatus { get; set; }
    public Guid ChangedByPersonId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}