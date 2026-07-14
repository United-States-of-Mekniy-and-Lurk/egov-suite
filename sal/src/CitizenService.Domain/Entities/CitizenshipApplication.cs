using System.Text.Json;
using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Entities;

public class CitizenshipApplication
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public ApplicationStatus Status { get; set; }
    public string FormName { get; set; } = string.Empty;
    public int FormVersion { get; set; }
    public JsonDocument? FormAnswers { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionReason { get; set; }
    public Guid? ReviewerPersonId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByPersonId { get; set; }
}
