using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Domain.Entities;

public sealed class OrganizationCorrectionRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid RequestedByPersonId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public string? ProposedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CorrectionRequestStatus Status { get; set; } = CorrectionRequestStatus.Submitted;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByPersonId { get; set; }
    public string? ReviewReason { get; set; }
}