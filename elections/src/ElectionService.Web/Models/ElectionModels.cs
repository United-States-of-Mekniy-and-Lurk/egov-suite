using System.ComponentModel.DataAnnotations;

namespace ElectionService.Web.Models;

public sealed record CandidateView(Guid Id, Guid? PersonId, string DisplayName, string? Description, int Position, DateTime? WithdrawnAt, bool IsWinner);

public sealed record PartyListView(
    Guid Id,
    Guid? PartyOrganizationId,
    string PartyRegistrationNumber,
    string PartyName,
    string ListName,
    int SortOrder,
    IReadOnlyList<CandidateView> Candidates);

public sealed record ReferendumOptionView(Guid Id, string Code, string Label, string? Description, int SortOrder);

public sealed record ElectionView(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string Type,
    string Status,
    string EligibilityMode,
    DateTime VotingStartsAt,
    DateTime VotingEndsAt,
    string? TerritoryCode,
    int? SeatCount,
    IReadOnlyList<PartyListView> PartyLists,
    IReadOnlyList<ReferendumOptionView> ReferendumOptions,
    bool IsHistorical,
    bool IsPubliclyVisible,
    string? HistoricalSourceReference)
{
    public bool IsOpen => Status == "Published" && DateTime.UtcNow >= VotingStartsAt && DateTime.UtcNow < VotingEndsAt;
    public bool HasPublicResults => Status is "Finalized" or "Certified" or "Archived";
}

public sealed record ResultView(string SelectionType, Guid SelectionId, string SelectionLabel, string? TerritoryCode, int VoteCount);
public sealed record TurnoutView(
    int? EligibleVoterCount,
    int ParticipatingVoterCount,
    int ValidBallotCount,
    int InvalidBallotCount,
    decimal? TurnoutPercentage);
public sealed record OfficialElectionRecordView(
    ElectionView Election,
    TurnoutView Turnout,
    IReadOnlyList<ResultView> Results);
public sealed record BallotReceipt(Guid ElectionId, DateOnly RecordedOn);
public sealed record InvitationCreated(Guid Id, string Token, string? Label);
public sealed record VoterRollEntryView(Guid PersonId, DateTime AddedAt, Guid AddedByPersonId);
public sealed record InvitationAdminView(Guid Id, string Token, string? Label, Guid? PersonId, DateTime CreatedAt,
    Guid CreatedByPersonId, DateOnly? UsedOn, DateTime? RevokedAt);
public sealed record InvitationDetail(Guid ElectionId, string ElectionTitle, DateTime VotingStartsAt, DateTime VotingEndsAt, bool IsAvailable);

public sealed class ElectionInput
{
    [Display(Name = "Record slug"), Required, StringLength(120)] public string Slug { get; set; } = string.Empty;
    [Display(Name = "Title"), Required, StringLength(240)] public string Title { get; set; } = string.Empty;
    [Display(Name = "Description"), Required, StringLength(4000)] public string Description { get; set; } = string.Empty;
    [Display(Name = "Type"), Required] public string Type { get; set; } = "PartyList";
    [Display(Name = "Eligibility"), Required] public string EligibilityMode { get; set; } = "AllActiveCitizens";
    [Display(Name = "Voting starts"), Required] public DateTime VotingStartsAt { get; set; }
    [Display(Name = "Voting ends"), Required] public DateTime VotingEndsAt { get; set; }
    [Display(Name = "Territory code"), StringLength(80)] public string? TerritoryCode { get; set; }
    [Display(Name = "Eligible voters (optional)"), Range(0, int.MaxValue)] public int? EligibleVoterCount { get; set; }
    [Display(Name = "Seats (optional)"), Range(1, int.MaxValue)] public int? SeatCount { get; set; }

    private static readonly TimeZoneInfo Prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    /// <summary>Convert VotingStartsAt/VotingEndsAt from Prague local to UTC DateTimeOffset for the API.</summary>
    public DateTimeOffset VotingStartsAtUtc => new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(VotingStartsAt, DateTimeKind.Unspecified), Prague), TimeSpan.Zero);
    public DateTimeOffset VotingEndsAtUtc => new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(VotingEndsAt, DateTimeKind.Unspecified), Prague), TimeSpan.Zero);

    public static DateTime UtcToPrague(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Prague);
}

public sealed record HistoricalCandidateInput(Guid? PersonId, string DisplayName, string? Description, int Position);
public sealed record HistoricalPartyListInput(
    Guid? PartyOrganizationId,
    string PartyRegistrationNumber,
    string PartyName,
    string ListName,
    int SortOrder,
    IReadOnlyList<HistoricalCandidateInput>? Candidates,
    int VoteCount);
public sealed record HistoricalReferendumOptionInput(
    string Code,
    string Label,
    string? Description,
    int SortOrder,
    int VoteCount);
public sealed record HistoricalElectionInput(
    string Slug,
    string Title,
    string Description,
    string Type,
    DateTimeOffset VotingStartsAt,
    DateTimeOffset VotingEndsAt,
    string? TerritoryCode,
    string SourceReference,
    int? EligibleVoterCount,
    int ParticipatingVoterCount,
    int InvalidBallotCount,
    IReadOnlyList<HistoricalPartyListInput>? PartyLists,
    IReadOnlyList<HistoricalReferendumOptionInput>? ReferendumOptions);

public sealed class PartyListInput : IValidatableObject
{
    [Display(Name = "Party organization ID")] public Guid? PartyOrganizationId { get; set; }
    [Display(Name = "Independent party name"), StringLength(240)] public string? PartyName { get; set; }
    [Display(Name = "Registration number"), StringLength(100)] public string? PartyRegistrationNumber { get; set; }
    [Display(Name = "List name"), Required, StringLength(240)] public string ListName { get; set; } = string.Empty;
    [Display(Name = "Sort order")] public int SortOrder { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PartyOrganizationId is null && string.IsNullOrWhiteSpace(PartyName))
        {
            yield return new ValidationResult(
                "Enter a registered party organization ID or an independent party name.",
                [nameof(PartyOrganizationId), nameof(PartyName)]);
        }
    }
}

public sealed class CandidateInput : IValidatableObject
{
    [Display(Name = "Person ID")] public Guid? PersonId { get; set; }
    [Display(Name = "Display name"), StringLength(240)] public string? DisplayName { get; set; }
    [Display(Name = "Description"), StringLength(2000)] public string? Description { get; set; }
    [Display(Name = "Position")] public int Position { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PersonId is null && string.IsNullOrWhiteSpace(DisplayName))
        {
            yield return new ValidationResult(
                "Enter a registered person ID or a display name.",
                [nameof(PersonId), nameof(DisplayName)]);
        }
    }
}

public sealed class ReferendumOptionInput
{
    [Display(Name = "Code"), Required, StringLength(80)] public string Code { get; set; } = string.Empty;
    [Display(Name = "Label"), Required, StringLength(240)] public string Label { get; set; } = string.Empty;
    [Display(Name = "Description"), StringLength(2000)] public string? Description { get; set; }
    [Display(Name = "Sort order")] public int SortOrder { get; set; }
}

public sealed class VoterRollInput { [Display(Name = "Person ID"), Required] public Guid PersonId { get; set; } }
public sealed record BulkVoterRollInput(IReadOnlyList<Guid> PersonIds);
public sealed class InvitationInput { [Display(Name = "Person ID")] public Guid? PersonId { get; set; } [Display(Name = "Label"), StringLength(240)] public string? Label { get; set; } }
public sealed record BulkInvitationInput(IReadOnlyList<InvitationInput> Items);
public sealed class VoteInput { [Display(Name = "Ballot selection"), Required] public Guid SelectionId { get; set; } }

public sealed record TabularResultsView(
    Guid ElectionId,
    string Title,
    string Status,
    int TotalValidBallots,
    int ParticipatingVoters,
    int? EligibleVoters,
    decimal? TurnoutPercentage,
    bool IsLive,
    DateTime GeneratedAt,
    int? SeatCount,
    int WinnerCount,
    IReadOnlyList<TabularResultRow> Rows,
    IReadOnlyList<PartyResultGroup> PartyGroups);

public sealed record TabularResultRow(
    Guid SelectionId,
    string SelectionLabel,
    string SelectionType,
    string? PartyName,
    int VoteCount,
    decimal Percentage,
    string? TerritoryCode);

public sealed record CandidateResultView(Guid Id, string DisplayName, int Position, bool IsWithdrawn, bool IsWinner);
public sealed record PartyResultGroup(Guid PartyListId, string PartyName, string ListName, int VoteCount,
    decimal Percentage, IReadOnlyList<CandidateResultView> Candidates);
public sealed record WinnerSelectionInput(IReadOnlyList<Guid> CandidateIds);

public sealed record ReceiptVerificationResult(bool IsValid, Guid ElectionId);

public sealed record CertificationView(int ApprovalCount, int RejectionCount, int Quorum, bool IsCertified, DateTime? CertifiedAt);
public sealed class TransitionInput { [Display(Name = "Status"), Required] public string Status { get; set; } = string.Empty; [Display(Name = "Reason"), StringLength(1000)] public string? Reason { get; set; } }
public sealed record ElectionVisibilityInput(bool IsPubliclyVisible);