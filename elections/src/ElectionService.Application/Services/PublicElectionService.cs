using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Enums;

namespace ElectionService.Application.Services;

public sealed class PublicElectionService(IElectionStore store, ICredentialHashService hashes)
{
    public async Task<IReadOnlyList<ElectionView>> ListAsync(CancellationToken ct) =>
        (await store.ListPublicAsync(ct)).Select(ElectionMapping.ToView).ToList();

    public async Task<ElectionView> GetAsync(string identifier, CancellationToken ct) =>
        (await store.GetPublicAsync(identifier, ct) ?? throw new ElectionNotFoundException("Election was not found.")).ToView();

    public async Task<IReadOnlyList<ResultView>> ResultsAsync(Guid electionId, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status is not (ElectionStatus.Finalized or ElectionStatus.Archived))
            throw new ElectionNotFoundException("Election results are not available.");
        return (await store.GetResultsAsync(electionId, ct)).Select(item => new ResultView(
            item.SelectionType, item.SelectionId, item.SelectionLabel, item.TerritoryCode, item.VoteCount)).ToList();
    }

    public async Task<OfficialElectionRecordView> RecordAsync(string identifier, CancellationToken ct)
    {
        var election = await store.GetPublicAsync(identifier, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status is not (ElectionStatus.Finalized or ElectionStatus.Archived))
            throw new ElectionNotFoundException("Election official record is not available.");

        var results = (await store.GetResultsAsync(election.Id, ct)).Select(item => new ResultView(
            item.SelectionType, item.SelectionId, item.SelectionLabel, item.TerritoryCode, item.VoteCount)).ToList();
        int participatingVoterCount;
        int validBallotCount;
        int invalidBallotCount;
        if (election.IsHistorical)
        {
            participatingVoterCount = election.HistoricalParticipatingVoterCount
                ?? throw new ElectionValidationException("Historical turnout data is incomplete.");
            invalidBallotCount = election.HistoricalInvalidBallotCount
                ?? throw new ElectionValidationException("Historical invalid ballot data is incomplete.");
            validBallotCount = results.Sum(item => item.VoteCount);
        }
        else
        {
            var counts = await store.GetLiveAggregateCountsAsync(election.Id, ct);
            participatingVoterCount = counts.ParticipatingVoterCount;
            validBallotCount = counts.ValidBallotCount;
            invalidBallotCount = 0;
        }
        var turnoutPercentage = election.EligibleVoterCount is > 0
            ? Math.Round(participatingVoterCount * 100m / election.EligibleVoterCount.Value, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;
        return new OfficialElectionRecordView(
            election.ToView(),
            new TurnoutView(election.EligibleVoterCount, participatingVoterCount, validBallotCount,
                invalidBallotCount, turnoutPercentage),
            results);
    }

    public async Task<InvitationDetail> InvitationAsync(Guid electionId, string token, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        var hash = hashes.HashInvitation(electionId, token, election.CredentialHashKeyVersion);
        var invitation = await store.GetInvitationAsync(electionId, hash, ct)
            ?? throw new ElectionNotFoundException("Invitation was not found.");
        var now = DateTime.UtcNow;
        return new InvitationDetail(election.Id, election.Title, election.VotingStartsAt, election.VotingEndsAt,
            invitation.UsedOn is null && invitation.RevokedAt is null && election.Status == ElectionStatus.Published &&
            now >= election.VotingStartsAt && now < election.VotingEndsAt);
    }
}