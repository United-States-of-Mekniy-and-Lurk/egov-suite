namespace ElectionService.Domain.Entities;

public sealed class PartyList
{
    public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Election Election { get; set; } = null!;
    public Guid? PartyOrganizationId { get; set; }
    public string PartyRegistrationNumber { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string ListName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<Candidate> Candidates { get; set; } = [];
}