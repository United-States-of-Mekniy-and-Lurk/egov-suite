using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;

namespace ElectionService.Application.Abstractions;

public interface IElectionStore
{
    Task<IReadOnlyList<Election>> ListAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Election>> ListPublicAsync(CancellationToken ct);
    Task<Election?> GetAsync(Guid id, CancellationToken ct);
    Task<Election?> GetPublicAsync(string identifier, CancellationToken ct);
    Task<IReadOnlyList<ElectionResult>> GetResultsAsync(Guid electionId, CancellationToken ct);
    Task<ElectionAggregateCounts> GetLiveAggregateCountsAsync(Guid electionId, CancellationToken ct);
    Task<VotingInvitation?> GetInvitationAsync(Guid electionId, string tokenHash, CancellationToken ct);
    Task AddElectionAsync(Election election, CancellationToken ct);
    Task ImportHistoricalAsync(Election election, IReadOnlyList<ElectionResult> results,
        ElectionTransition transition, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptElectionId, CancellationToken ct);
    Task<bool> IsOnVoterRollAsync(Guid electionId, Guid personId, CancellationToken ct);
    Task<IReadOnlyList<VoterRollEntry>> ListVoterRollAsync(Guid electionId, CancellationToken ct);
    Task<IReadOnlyList<VotingInvitation>> ListInvitationsAsync(Guid electionId, CancellationToken ct);
    Task AddPartyListAsync(PartyList partyList, CancellationToken ct);
    Task AddCandidateAsync(Candidate candidate, CancellationToken ct);
    Task AddReferendumOptionAsync(ReferendumOption option, CancellationToken ct);
    Task AddVoterRollEntryAsync(VoterRollEntry entry, CancellationToken ct);
    Task AddVoterRollEntriesAsync(IReadOnlyList<VoterRollEntry> entries, CancellationToken ct);
    Task AddInvitationAsync(VotingInvitation invitation, CancellationToken ct);
    Task AddInvitationsAsync(IReadOnlyList<VotingInvitation> invitations, CancellationToken ct);
    Task RemovePartyListAsync(PartyList partyList, CancellationToken ct);
    Task RemoveCandidateAsync(Candidate candidate, CancellationToken ct);
    Task RemoveReferendumOptionAsync(ReferendumOption option, CancellationToken ct);
    Task<bool> RemoveVoterRollEntryAsync(Guid electionId, Guid personId, CancellationToken ct);
    Task<VotingInvitation?> GetInvitationByIdAsync(Guid electionId, Guid invitationId, CancellationToken ct);
    Task TransitionAsync(Guid electionId, ElectionStatus target, Guid actorPersonId, string? reason, DateTime now, CancellationToken ct);
    Task FinalizeAsync(Guid electionId, Guid actorPersonId, string? reason, DateTime now, CancellationToken ct);
    Task SubmitBallotAsync(SubmitBallotCommand command, DateTime now, CancellationToken ct);
}

public interface ICredentialHashService
{
    string ActiveKeyVersion { get; }
    string HashCitizen(Guid electionId, Guid personId, string keyVersion);
    (string Token, string Hash) CreateInvitation(Guid electionId, string keyVersion);
    string HashInvitation(Guid electionId, string token, string keyVersion);
}

public interface IOrganizationRegistryClient
{
    Task<OrganizationSnapshot?> GetAsync(Guid organizationId, CancellationToken ct);
}

public interface ICitizenRegistryClient
{
    Task<CitizenSnapshot?> GetAsync(Guid personId, CancellationToken ct);
}

public interface IPersonRegistryClient
{
    Task<PersonSnapshot?> GetAsync(Guid personId, CancellationToken ct);
}