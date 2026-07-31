using ElectionService.Domain.Enums;

namespace ElectionService.Domain.Entities;

public sealed class Election
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ElectionType Type { get; set; }
    public ElectionStatus Status { get; set; } = ElectionStatus.Draft;
    public EligibilityMode EligibilityMode { get; set; }
    public string CredentialHashKeyVersion { get; set; } = string.Empty;
    public DateTime VotingStartsAt { get; set; }
    public DateTime VotingEndsAt { get; set; }
    public string? TerritoryCode { get; set; }
    public int? EligibleVoterCount { get; set; }
    public int? SeatCount { get; set; }
    public int? HistoricalParticipatingVoterCount { get; set; }
    public int? HistoricalInvalidBallotCount { get; set; }
    public bool IsHistorical { get; set; }
    public string? HistoricalSourceReference { get; set; }
    public DateTime? ImportedAt { get; set; }
    public Guid? ImportedByPersonId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime? CertifiedAt { get; set; }
    public int CertificationQuorum { get; set; } = 2;
    public Guid CreatedByPersonId { get; set; }
    public List<PartyList> PartyLists { get; set; } = [];
    public List<ReferendumOption> ReferendumOptions { get; set; } = [];
}