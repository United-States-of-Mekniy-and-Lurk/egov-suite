using ElectionService.Domain.Enums;
using System.Text.Json.Serialization;

namespace ElectionService.Application.Models;

public sealed record ElectionInput(
    string Slug,
    string Title,
    string Description,
    ElectionType Type,
    EligibilityMode EligibilityMode,
    DateTime VotingStartsAt,
    DateTime VotingEndsAt,
    string? TerritoryCode,
    int? EligibleVoterCount = null);

public sealed record PartyListInput(
    Guid? PartyOrganizationId,
    string? PartyName,
    string? PartyRegistrationNumber,
    string ListName,
    int SortOrder);
public sealed record CandidateInput(Guid? PersonId, string? DisplayName, string? Description, int Position);
public sealed record ReferendumOptionInput(string Code, string Label, string? Description, int SortOrder);
public sealed record VoterRollInput(Guid PersonId);
public sealed record BulkVoterRollInput(IReadOnlyList<Guid> PersonIds);
public sealed record InvitationInput(Guid? PersonId, string? Label);
public sealed record BulkInvitationInput(IReadOnlyList<InvitationInput> Items);
public sealed record VoteInput(Guid SelectionId);
public sealed record TransitionInput(ElectionStatus Status, string? Reason);
public sealed record ScheduleInput(DateTime VotingStartsAt, DateTime VotingEndsAt);

public sealed record CandidateView(Guid Id, string DisplayName, string? Description, int Position, bool IsWithdrawn);
public sealed record CandidateAdminView(Guid Id, Guid? PersonId, string DisplayName, string? Description, int Position, DateTime? WithdrawnAt);
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
    ElectionType Type,
    ElectionStatus Status,
    EligibilityMode EligibilityMode,
    DateTime VotingStartsAt,
    DateTime VotingEndsAt,
    string? TerritoryCode,
    IReadOnlyList<PartyListView> PartyLists,
    IReadOnlyList<ReferendumOptionView> ReferendumOptions,
    bool IsHistorical,
    string? HistoricalSourceReference);
public sealed record PartyListAdminView(
    Guid Id,
    Guid? PartyOrganizationId,
    string PartyRegistrationNumber,
    string PartyName,
    string ListName,
    int SortOrder,
    IReadOnlyList<CandidateAdminView> Candidates);
public sealed record AdminElectionView(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    ElectionType Type,
    ElectionStatus Status,
    EligibilityMode EligibilityMode,
    DateTime VotingStartsAt,
    DateTime VotingEndsAt,
    string? TerritoryCode,
    IReadOnlyList<PartyListAdminView> PartyLists,
    IReadOnlyList<ReferendumOptionView> ReferendumOptions,
    int? EligibleVoterCount,
    bool IsHistorical,
    string? HistoricalSourceReference,
    DateTime? ImportedAt,
    Guid? ImportedByPersonId);
public sealed record ResultView(SelectionType SelectionType, Guid SelectionId, string SelectionLabel, string? TerritoryCode, int VoteCount);
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

public sealed record HistoricalCandidateInput(
    Guid? PersonId,
    [property: JsonRequired] string DisplayName,
    string? Description,
    [property: JsonRequired] int Position);
public sealed record HistoricalPartyListInput(
    Guid? PartyOrganizationId,
    [property: JsonRequired] string PartyRegistrationNumber,
    [property: JsonRequired] string PartyName,
    [property: JsonRequired] string ListName,
    [property: JsonRequired] int SortOrder,
    IReadOnlyList<HistoricalCandidateInput>? Candidates,
    [property: JsonRequired] int VoteCount);
public sealed record HistoricalReferendumOptionInput(
    [property: JsonRequired] string Code,
    [property: JsonRequired] string Label,
    string? Description,
    [property: JsonRequired] int SortOrder,
    [property: JsonRequired] int VoteCount);
public sealed record HistoricalElectionInput(
    [property: JsonRequired] string Slug,
    [property: JsonRequired] string Title,
    [property: JsonRequired] string Description,
    [property: JsonRequired] ElectionType Type,
    [property: JsonRequired] DateTime VotingStartsAt,
    [property: JsonRequired] DateTime VotingEndsAt,
    string? TerritoryCode,
    [property: JsonRequired] string SourceReference,
    int? EligibleVoterCount,
    [property: JsonRequired] int ParticipatingVoterCount,
    [property: JsonRequired] int InvalidBallotCount,
    IReadOnlyList<HistoricalPartyListInput>? PartyLists,
    IReadOnlyList<HistoricalReferendumOptionInput>? ReferendumOptions);
public sealed record ElectionAggregateCounts(int ParticipatingVoterCount, int ValidBallotCount);
public sealed record BallotReceipt(Guid ElectionId, DateOnly RecordedOn);
public sealed record InvitationCreated(Guid Id, string Token, string? Label);
public sealed record VoterRollEntryView(Guid PersonId, DateTime AddedAt, Guid AddedByPersonId);
public sealed record InvitationAdminView(
    Guid Id,
    string? Label,
    Guid? PersonId,
    DateTime CreatedAt,
    Guid CreatedByPersonId,
    DateOnly? UsedOn,
    DateTime? RevokedAt);
public sealed record InvitationDetail(Guid ElectionId, string ElectionTitle, DateTime VotingStartsAt, DateTime VotingEndsAt, bool IsAvailable);

public sealed record OrganizationSnapshot(
    Guid Id,
    string RegistrationNumber,
    string LegalName,
    string Status,
    IReadOnlyList<string> ClassificationCodes);
public sealed record CitizenSnapshot(Guid PersonId, string Status);
public sealed record PersonSnapshot(Guid Id, string DisplayName);

public sealed record SubmitBallotCommand(
    Guid ElectionId,
    ParticipationChannel Channel,
    string CredentialHash,
    Guid SelectionId,
    DateOnly RecordedOn,
    Guid? InvitationId,
    Guid? CitizenPersonId);