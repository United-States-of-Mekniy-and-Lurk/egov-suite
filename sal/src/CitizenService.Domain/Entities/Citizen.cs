using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Entities;

public class Citizen
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string CitizenNumber { get; set; } = string.Empty;
    public CitizenStatus Status { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ImportSource { get; set; }
    public Guid CreatedByPersonId { get; set; }
}
