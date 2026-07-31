using System.Data;
using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElectionService.Infrastructure.Persistence;

public sealed class ElectionStore(ElectionDbContext db) : IElectionStore
{
    public async Task<IReadOnlyList<Election>> ListAllAsync(CancellationToken ct) =>
        await FullQuery().AsNoTracking().OrderByDescending(item => item.VotingStartsAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Election>> ListPublicAsync(CancellationToken ct) =>
        await FullQuery().AsNoTracking().Where(item => item.Status != ElectionStatus.Draft && item.IsPubliclyVisible)
            .OrderByDescending(item => item.VotingStartsAt).ToListAsync(ct);

    public Task<Election?> GetAsync(Guid id, CancellationToken ct) =>
        FullQuery().SingleOrDefaultAsync(item => item.Id == id, ct);

    public Task<Election?> GetPublicAsync(string identifier, CancellationToken ct)
    {
        var isId = Guid.TryParse(identifier, out var id);
        return FullQuery().AsNoTracking().SingleOrDefaultAsync(item =>
            item.Status != ElectionStatus.Draft && item.IsPubliclyVisible &&
            (isId ? item.Id == id : item.Slug == identifier), ct);
    }

    public async Task<IReadOnlyList<ElectionResult>> GetResultsAsync(Guid electionId, CancellationToken ct) =>
        await db.ElectionResults.AsNoTracking().Where(item => item.ElectionId == electionId)
            .OrderBy(item => item.SelectionType).ThenByDescending(item => item.VoteCount).ToListAsync(ct);

    public async Task<ElectionAggregateCounts> GetLiveAggregateCountsAsync(Guid electionId, CancellationToken ct) =>
        new(
            await db.ParticipationRecords.CountAsync(item => item.ElectionId == electionId, ct),
            await db.AnonymousBallots.CountAsync(item => item.ElectionId == electionId, ct));

    public async Task<IReadOnlyList<ElectionSelectionCount>> GetLiveSelectionCountsAsync(Guid electionId, CancellationToken ct) =>
        await db.AnonymousBallots.AsNoTracking().Where(item => item.ElectionId == electionId)
            .GroupBy(item => new { item.SelectionType, item.SelectionId, item.TerritoryCode })
            .Select(group => new ElectionSelectionCount(
                group.Key.SelectionType, group.Key.SelectionId, group.Key.TerritoryCode, group.Count()))
            .ToListAsync(ct);

    public Task<VotingInvitation?> GetInvitationAsync(Guid electionId, string tokenHash, CancellationToken ct) =>
        db.VotingInvitations.AsNoTracking().SingleOrDefaultAsync(item => item.ElectionId == electionId && item.TokenHash == tokenHash, ct);

    public async Task AddElectionAsync(Election election, CancellationToken ct)
    {
        db.Elections.Add(election);
        await db.SaveChangesAsync(ct);
    }

    public async Task ImportHistoricalAsync(Election election, IReadOnlyList<ElectionResult> results,
        ElectionTransition transition, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        db.Elections.Add(election);
        db.ElectionResults.AddRange(results);
        db.ElectionTransitions.Add(transition);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<bool> SlugExistsAsync(string slug, Guid? exceptElectionId, CancellationToken ct) =>
        db.Elections.AnyAsync(item => item.Slug == slug && (!exceptElectionId.HasValue || item.Id != exceptElectionId), ct);
    public Task<bool> IsOnVoterRollAsync(Guid electionId, Guid personId, CancellationToken ct) =>
        db.VoterRollEntries.AnyAsync(item => item.ElectionId == electionId && item.PersonId == personId, ct);
    public async Task<IReadOnlyList<VoterRollEntry>> ListVoterRollAsync(Guid electionId, CancellationToken ct) =>
        await db.VoterRollEntries.AsNoTracking().Where(item => item.ElectionId == electionId)
            .OrderBy(item => item.AddedAt).ToListAsync(ct);
    public async Task<IReadOnlyList<VotingInvitation>> ListInvitationsAsync(Guid electionId, CancellationToken ct) =>
        await db.VotingInvitations.AsNoTracking().Where(item => item.ElectionId == electionId)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(ct);

    public async Task AddPartyListAsync(PartyList partyList, CancellationToken ct) { db.PartyLists.Add(partyList); await db.SaveChangesAsync(ct); }
    public async Task AddCandidateAsync(Candidate candidate, CancellationToken ct) { db.Candidates.Add(candidate); await db.SaveChangesAsync(ct); }
    public async Task AddReferendumOptionAsync(ReferendumOption option, CancellationToken ct) { db.ReferendumOptions.Add(option); await db.SaveChangesAsync(ct); }
    public async Task AddVoterRollEntryAsync(VoterRollEntry entry, CancellationToken ct) { db.VoterRollEntries.Add(entry); await db.SaveChangesAsync(ct); }
    public async Task AddVoterRollEntriesAsync(IReadOnlyList<VoterRollEntry> entries, CancellationToken ct) { db.VoterRollEntries.AddRange(entries); await db.SaveChangesAsync(ct); }
    public async Task AddInvitationAsync(VotingInvitation invitation, CancellationToken ct) { db.VotingInvitations.Add(invitation); await db.SaveChangesAsync(ct); }
    public async Task AddInvitationsAsync(IReadOnlyList<VotingInvitation> invitations, CancellationToken ct) { db.VotingInvitations.AddRange(invitations); await db.SaveChangesAsync(ct); }
    public async Task RemovePartyListAsync(PartyList partyList, CancellationToken ct) { db.PartyLists.Remove(partyList); await db.SaveChangesAsync(ct); }
    public async Task RemoveCandidateAsync(Candidate candidate, CancellationToken ct) { db.Candidates.Remove(candidate); await db.SaveChangesAsync(ct); }
    public async Task RemoveReferendumOptionAsync(ReferendumOption option, CancellationToken ct) { db.ReferendumOptions.Remove(option); await db.SaveChangesAsync(ct); }
    public async Task<bool> RemoveVoterRollEntryAsync(Guid electionId, Guid personId, CancellationToken ct)
    {
        var entry = await db.VoterRollEntries.SingleOrDefaultAsync(item => item.ElectionId == electionId && item.PersonId == personId, ct);
        if (entry is null) return false;
        db.VoterRollEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        return true;
    }
    public Task<VotingInvitation?> GetInvitationByIdAsync(Guid electionId, Guid invitationId, CancellationToken ct) =>
        db.VotingInvitations.SingleOrDefaultAsync(item => item.ElectionId == electionId && item.Id == invitationId, ct);

    public async Task TransitionAsync(Guid electionId, ElectionStatus target, Guid actorPersonId, string? reason, DateTime now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var election = await db.Elections.SingleAsync(item => item.Id == electionId, ct);
        var previous = election.Status;
        election.Status = target;
        election.UpdatedAt = now;
        if (target == ElectionStatus.Published) election.PublishedAt = now;
        if (target == ElectionStatus.Closed) election.ClosedAt = now;
        if (target == ElectionStatus.Certified) election.CertifiedAt = now;
        db.ElectionTransitions.Add(NewTransition(electionId, previous, target, actorPersonId, reason, now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task FinalizeAsync(Guid electionId, Guid actorPersonId, string? reason, DateTime now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var election = await FullQuery().SingleAsync(item => item.Id == electionId, ct);
        if (election.Status != ElectionStatus.Closed)
            throw new ElectionValidationException("Only a closed election can be finalized.");
        if (await db.ElectionResults.AnyAsync(item => item.ElectionId == electionId, ct))
            throw new ElectionConflictException("Election results have already been finalized.");

        if (election.EligibilityMode == EligibilityMode.SpecificVoterRoll && !election.EligibleVoterCount.HasValue)
            election.EligibleVoterCount = await db.VoterRollEntries.CountAsync(item => item.ElectionId == electionId, ct);

        var partyLabels = election.PartyLists.ToDictionary(item => item.Id, item => item.ListName);
        var optionLabels = election.ReferendumOptions.ToDictionary(item => item.Id, item => item.Label);
        var aggregates = await db.AnonymousBallots.Where(item => item.ElectionId == electionId)
            .GroupBy(item => new { item.SelectionType, item.SelectionId, item.TerritoryCode })
            .Select(group => new { group.Key.SelectionType, group.Key.SelectionId, group.Key.TerritoryCode, VoteCount = group.Count() })
            .ToListAsync(ct);
        var counts = aggregates.ToDictionary(
            item => (item.SelectionType, item.SelectionId, item.TerritoryCode),
            item => item.VoteCount);
        foreach (var partyList in election.PartyLists)
            counts.TryAdd((SelectionType.PartyList, partyList.Id, election.TerritoryCode), 0);
        foreach (var option in election.ReferendumOptions)
            counts.TryAdd((SelectionType.ReferendumOption, option.Id, election.TerritoryCode), 0);

        foreach (var result in counts)
        {
            var label = result.Key.SelectionType == SelectionType.PartyList
                ? partyLabels[result.Key.SelectionId]
                : optionLabels[result.Key.SelectionId];
            db.ElectionResults.Add(new ElectionResult
            {
                Id = Guid.NewGuid(), ElectionId = electionId, SelectionType = result.Key.SelectionType,
                SelectionId = result.Key.SelectionId, SelectionLabel = label, TerritoryCode = result.Key.TerritoryCode,
                VoteCount = result.Value, FinalizedAt = now
            });
        }
        election.Status = ElectionStatus.Finalized;
        election.FinalizedAt = now;
        election.UpdatedAt = now;
        db.ElectionTransitions.Add(NewTransition(electionId, ElectionStatus.Closed, ElectionStatus.Finalized, actorPersonId, reason, now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<string> SubmitBallotAsync(SubmitBallotCommand command, DateTime now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var election = await db.Elections.SingleOrDefaultAsync(item => item.Id == command.ElectionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status != ElectionStatus.Published || now < election.VotingStartsAt || now >= election.VotingEndsAt)
            throw new ElectionValidationException("Election is not open for voting.");
        if (await db.ParticipationRecords.AnyAsync(item => item.ElectionId == command.ElectionId &&
            item.Channel == command.Channel && item.CredentialHash == command.CredentialHash, ct))
            throw new ElectionConflictException("This voting credential has already been used.");
        if (command.Channel == ParticipationChannel.Citizen)
        {
            if (!command.CitizenPersonId.HasValue)
                throw new ElectionValidationException("Citizen identity is required for eligibility validation.");
            if (election.EligibilityMode == EligibilityMode.SpecificVoterRoll &&
                !await db.VoterRollEntries.AnyAsync(item => item.ElectionId == command.ElectionId &&
                    item.PersonId == command.CitizenPersonId.Value, ct))
                throw new ElectionForbiddenException("The citizen is not eligible for this election.");
        }

        SelectionType selectionType;
        if (election.Type == ElectionType.PartyList && await db.PartyLists.AnyAsync(item => item.ElectionId == command.ElectionId && item.Id == command.SelectionId, ct))
            selectionType = SelectionType.PartyList;
        else if (election.Type == ElectionType.Referendum && await db.ReferendumOptions.AnyAsync(item => item.ElectionId == command.ElectionId && item.Id == command.SelectionId, ct))
            selectionType = SelectionType.ReferendumOption;
        else
            throw new ElectionValidationException("Selection is not valid for this election.");

        VotingInvitation? invitation = null;
        if (command.Channel == ParticipationChannel.Invitation)
        {
            if (!command.InvitationId.HasValue)
                throw new ElectionValidationException("Invitation is required.");
            invitation = await db.VotingInvitations.SingleOrDefaultAsync(item => item.Id == command.InvitationId &&
                item.ElectionId == command.ElectionId && item.TokenHash == command.CredentialHash, ct);
            if (invitation is null || invitation.UsedOn is not null || invitation.RevokedAt is not null)
                throw new ElectionConflictException("Invitation is invalid or has already been used.");
        }

        var ballotId = Guid.NewGuid();
        var receiptHash = ComputeReceiptHash(command.ElectionId, ballotId);
        db.AnonymousBallots.Add(new AnonymousBallot
        {
            Id = ballotId, ElectionId = command.ElectionId, SelectionType = selectionType,
            SelectionId = command.SelectionId, TerritoryCode = election.TerritoryCode,
            ReceiptHash = receiptHash
        });
        db.ParticipationRecords.Add(new ParticipationRecord
        {
            Id = Guid.NewGuid(), ElectionId = command.ElectionId, Channel = command.Channel,
            CredentialHash = command.CredentialHash, RecordedOn = command.RecordedOn
        });
        if (invitation is not null) invitation.UsedOn = command.RecordedOn;

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ElectionConflictException("This voting credential has already been used.");
        }

        return receiptHash;
    }

    private static string ComputeReceiptHash(Guid electionId, Guid ballotId)
    {
        var data = System.Text.Encoding.UTF8.GetBytes($"{electionId}:{ballotId}:{Guid.NewGuid()}");
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return Convert.ToHexStringLower(hash);
    }

    public Task<bool> VerifyReceiptAsync(Guid electionId, string receiptHash, CancellationToken ct) =>
        db.AnonymousBallots.AnyAsync(item => item.ElectionId == electionId && item.ReceiptHash == receiptHash, ct);

    public async Task AddCertificationDecisionAsync(CertificationDecision decision, CancellationToken ct)
    {
        await db.CertificationDecisions.AddAsync(decision, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CertificationDecision>> ListCertificationDecisionsAsync(Guid electionId, CancellationToken ct) =>
        await db.CertificationDecisions.Where(item => item.ElectionId == electionId).OrderBy(item => item.DecidedAt).ToListAsync(ct);

    private IQueryable<Election> FullQuery() => db.Elections
        .Include(item => item.PartyLists).ThenInclude(item => item.Candidates)
        .Include(item => item.ReferendumOptions);

    private static ElectionTransition NewTransition(Guid electionId, ElectionStatus from, ElectionStatus to,
        Guid actorPersonId, string? reason, DateTime now) => new()
        {
            Id = Guid.NewGuid(), ElectionId = electionId, FromStatus = from, ToStatus = to,
            ChangedByPersonId = actorPersonId, ChangedAt = now,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        };
}