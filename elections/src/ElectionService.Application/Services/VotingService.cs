using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Enums;
using Egov.Platform.Identity;

namespace ElectionService.Application.Services;

public sealed class VotingService(
    IElectionStore store,
    ICredentialHashService hashes,
    ICitizenRegistryClient citizens,
    ICurrentActor actor)
{
    public async Task<BallotReceipt> VoteAsCitizenAsync(Guid electionId, VoteInput input, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        var citizen = await citizens.GetAsync(actor.PersonId, ct);
        if (citizen?.Status != "Active")
            throw new ElectionForbiddenException("An active citizenship is required.");
        if (election.EligibilityMode == EligibilityMode.SpecificVoterRoll &&
            !await store.IsOnVoterRollAsync(electionId, actor.PersonId, ct))
            throw new ElectionForbiddenException("The citizen is not eligible for this election.");

        var now = DateTime.UtcNow;
        var recordedOn = DateOnly.FromDateTime(now);
        await store.SubmitBallotAsync(new SubmitBallotCommand(
            electionId, ParticipationChannel.Citizen,
            hashes.HashCitizen(electionId, actor.PersonId, election.CredentialHashKeyVersion),
            input.SelectionId, recordedOn, null, actor.PersonId), now, ct);
        return new BallotReceipt(electionId, recordedOn);
    }

    public async Task<BallotReceipt> VoteByInvitationAsync(Guid electionId, string token, VoteInput input, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        var tokenHash = hashes.HashInvitation(electionId, token, election.CredentialHashKeyVersion);
        var invitation = await store.GetInvitationAsync(electionId, tokenHash, ct)
            ?? throw new ElectionNotFoundException("Invitation was not found.");
        var now = DateTime.UtcNow;
        var recordedOn = DateOnly.FromDateTime(now);
        await store.SubmitBallotAsync(new SubmitBallotCommand(
            electionId, ParticipationChannel.Invitation, tokenHash, input.SelectionId, recordedOn, invitation.Id, null),
            now, ct);
        return new BallotReceipt(electionId, recordedOn);
    }
}