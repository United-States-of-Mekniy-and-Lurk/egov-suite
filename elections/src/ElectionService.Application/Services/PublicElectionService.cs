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
        if (election.Status is not (ElectionStatus.Finalized or ElectionStatus.Certified or ElectionStatus.Archived))
            throw new ElectionNotFoundException("Election results are not available.");
        return (await store.GetResultsAsync(electionId, ct)).Select(item => new ResultView(
            item.SelectionType, item.SelectionId, item.SelectionLabel, item.TerritoryCode, item.VoteCount)).ToList();
    }

    public async Task<OfficialElectionRecordView> RecordAsync(string identifier, CancellationToken ct)
    {
        var election = await store.GetPublicAsync(identifier, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status is not (ElectionStatus.Finalized or ElectionStatus.Certified or ElectionStatus.Archived))
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

    public async Task<IReadOnlyList<ElectionCalendarEntry>> CalendarAsync(CancellationToken ct)
    {
        var elections = await store.ListPublicAsync(ct);
        return elections.Select(e => new ElectionCalendarEntry(
            e.Id, e.Slug, e.Title, e.Type.ToString(), e.Status.ToString(),
            e.VotingStartsAt, e.VotingEndsAt, e.TerritoryCode)).ToList();
    }

    public async Task<TabularResultsView> TabularResultsAsync(Guid electionId, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");

        bool isLive;
        int totalValidBallots;
        int participatingVoters;
        IReadOnlyList<ElectionSelectionCount> countsBySelection;

        if (election.Status is ElectionStatus.Finalized or ElectionStatus.Certified or ElectionStatus.Archived)
        {
            isLive = false;
            var results = await store.GetResultsAsync(electionId, ct);
            totalValidBallots = results.Sum(r => r.VoteCount);
            countsBySelection = results.Select(r => new ElectionSelectionCount(
                r.SelectionType, r.SelectionId, r.TerritoryCode, r.VoteCount)).ToList();
            var counts = await store.GetLiveAggregateCountsAsync(electionId, ct);
            participatingVoters = election.IsHistorical
                ? election.HistoricalParticipatingVoterCount ?? counts.ParticipatingVoterCount
                : counts.ParticipatingVoterCount;
        }
        else if (election.Status == ElectionStatus.Published)
        {
            isLive = true;
            var counts = await store.GetLiveAggregateCountsAsync(electionId, ct);
            totalValidBallots = counts.ValidBallotCount;
            participatingVoters = counts.ParticipatingVoterCount;
            countsBySelection = await store.GetLiveSelectionCountsAsync(electionId, ct);
        }
        else
        {
            throw new ElectionNotFoundException("Results are not available for this election.");
        }

        var turnoutPct = election.EligibleVoterCount is > 0
            ? Math.Round(participatingVoters * 100m / election.EligibleVoterCount.Value, 2)
            : (decimal?)null;

        var countLookup = countsBySelection
            .GroupBy(item => item.SelectionId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.VoteCount));
        var partyGroups = election.PartyLists.OrderBy(item => item.SortOrder).Select(party =>
        {
            var voteCount = countLookup.GetValueOrDefault(party.Id);
            var percentage = totalValidBallots > 0 ? Math.Round(voteCount * 100m / totalValidBallots, 2) : 0;
            return new PartyResultGroup(
                party.Id, party.PartyName, party.ListName, voteCount, percentage,
                party.Candidates.OrderBy(candidate => candidate.Position).Select(candidate => new CandidateResultView(
                    candidate.Id, candidate.DisplayName, candidate.Position,
                    candidate.WithdrawnAt.HasValue, candidate.IsWinner)).ToList());
        }).ToList();
        var rows = election.Type == ElectionType.PartyList
            ? partyGroups.Select(group => new TabularResultRow(
                group.PartyListId, group.ListName, SelectionType.PartyList.ToString(), group.PartyName,
                group.VoteCount, group.Percentage, election.TerritoryCode)).ToList()
            : election.ReferendumOptions.OrderBy(item => item.SortOrder).Select(option =>
            {
                var voteCount = countLookup.GetValueOrDefault(option.Id);
                return new TabularResultRow(
                    option.Id, option.Label, SelectionType.ReferendumOption.ToString(), null, voteCount,
                    totalValidBallots > 0 ? Math.Round(voteCount * 100m / totalValidBallots, 2) : 0,
                    election.TerritoryCode);
            }).ToList();

        return new TabularResultsView(
            electionId, election.Title, election.Status.ToString(),
            totalValidBallots, participatingVoters, election.EligibleVoterCount,
            turnoutPct, isLive, DateTime.UtcNow, election.SeatCount,
            partyGroups.Sum(group => group.Candidates.Count(candidate => candidate.IsWinner)), rows, partyGroups);
    }

    public async Task<ReceiptVerificationResult> VerifyReceiptAsync(Guid electionId, string receiptHash, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status == ElectionStatus.Draft)
            throw new ElectionNotFoundException("Election was not found.");
        var isValid = await store.VerifyReceiptAsync(electionId, receiptHash, ct);
        return new ReceiptVerificationResult(isValid, electionId);
    }

    public async Task<CertificationView> GetCertificationStatusAsync(Guid electionId, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status is not (ElectionStatus.Finalized or ElectionStatus.Certified or ElectionStatus.Archived))
            throw new ElectionNotFoundException("Certification is not available for this election.");
        var decisions = await store.ListCertificationDecisionsAsync(electionId, ct);
        var approvals = decisions.Count(d => d.IsApproved);
        var rejections = decisions.Count(d => !d.IsApproved);
        return new CertificationView(approvals, rejections, election.CertificationQuorum,
            election.CertifiedAt.HasValue, election.CertifiedAt);
    }
}