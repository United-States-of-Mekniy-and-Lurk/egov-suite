using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Entities;

public class FieldCorrectionRequest
{
    public Guid Id { get; set; }
    public Guid CitizenId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public Guid RequestedByPersonId { get; set; }
    public string? CurrentValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string RequestReason { get; set; } = string.Empty;
    public FieldCorrectionStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public Guid? ReviewedByPersonId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}