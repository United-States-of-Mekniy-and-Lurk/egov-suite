using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Entities;

public class ApplicationTransition
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public ApplicationStatus FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public Guid ChangedByPersonId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
