using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using Egov.Platform.Identity;

namespace ElectionService.Application.Services;

public sealed class CertificationService(IElectionStore store, ICurrentActor actor)
{
    public async Task<CertificationView> CertifyAsync(Guid electionId, CertificationInput input, CancellationToken ct)
    {
        if (!actor.IsInRole("election-service:certifier"))
            throw new ElectionForbiddenException("A certifier role is required.");

        var election = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status != ElectionStatus.Finalized)
            throw new ElectionValidationException("Only finalized elections can be certified.");

        var existingDecisions = await store.ListCertificationDecisionsAsync(electionId, ct);
        if (existingDecisions.Any(d => d.CertifierPersonId == actor.PersonId))
            throw new ElectionConflictException("You have already submitted a certification decision for this election.");

        var decision = new CertificationDecision
        {
            Id = Guid.NewGuid(),
            ElectionId = electionId,
            CertifierPersonId = actor.PersonId,
            IsApproved = input.IsApproved,
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(),
            DecidedAt = DateTime.UtcNow
        };
        await store.AddCertificationDecisionAsync(decision, ct);

        // Check if quorum is met after this decision
        var allDecisions = await store.ListCertificationDecisionsAsync(electionId, ct);
        var approvalCount = allDecisions.Count(d => d.IsApproved);

        if (approvalCount >= election.CertificationQuorum)
        {
            var now = DateTime.UtcNow;
            await store.TransitionAsync(electionId, ElectionStatus.Certified, actor.PersonId,
                $"Quorum reached: {approvalCount}/{election.CertificationQuorum} certifiers approved.", now, ct);
        }

        var rejections = allDecisions.Count(d => !d.IsApproved);
        var refreshedElection = await store.GetAsync(electionId, ct);
        return new CertificationView(approvalCount, rejections, election.CertificationQuorum,
            refreshedElection?.CertifiedAt.HasValue ?? false, refreshedElection?.CertifiedAt);
    }
}
