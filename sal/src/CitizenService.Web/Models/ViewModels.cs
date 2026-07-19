namespace CitizenService.Web.Models;

public class PersonViewModel
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = string.Empty;
    public string PreferredUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CitizenViewModel
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string CitizenNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? GrantedAt { get; set; }
    public string? ImportSource { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplicationViewModel
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public int FormVersion { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
