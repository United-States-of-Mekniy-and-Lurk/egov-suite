namespace CitizenService.Web.Models;

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
