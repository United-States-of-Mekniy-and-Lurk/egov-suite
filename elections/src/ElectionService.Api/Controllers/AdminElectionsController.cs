using ElectionService.Application.Models;
using ElectionService.Application.Services;
using ElectionService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route("admin/elections")]
public sealed class AdminElectionsController(
    AdminElectionService service,
    PublicElectionService publicElectionService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminElectionView>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpGet("{electionId:guid}")]
    public Task<AdminElectionView> Get(Guid electionId, CancellationToken ct) => service.GetAsync(electionId, ct);

    [HttpGet("{electionId:guid}/results")]
    public Task<TabularResultsView> Results(Guid electionId, CancellationToken ct) =>
        publicElectionService.AdminTabularResultsAsync(electionId, ct);

    [HttpPost]
    public async Task<ActionResult<AdminElectionView>> Create([FromBody] ElectionInput input, CancellationToken ct)
    {
        var election = await service.CreateAsync(input, ct);
        return Created($"/public/elections/{election.Id}", election);
    }

    [HttpPut("{electionId:guid}")]
    public Task<AdminElectionView> Update(Guid electionId, [FromBody] ElectionInput input, CancellationToken ct) =>
        service.UpdateAsync(electionId, input, ct);

    [HttpPut("{electionId:guid}/visibility")]
    public Task<AdminElectionView> SetVisibility(
        Guid electionId,
        [FromBody] ElectionVisibilityInput input,
        CancellationToken ct) =>
        service.SetVisibilityAsync(electionId, input, ct);

    [HttpPost("{electionId:guid}/party-lists")]
    public Task<PartyListView> AddPartyList(Guid electionId, [FromBody] PartyListInput input, CancellationToken ct) =>
        service.AddPartyListAsync(electionId, input, ct);

    [HttpPut("{electionId:guid}/party-lists/{partyListId:guid}")]
    public Task<PartyListView> UpdatePartyList(Guid electionId, Guid partyListId, [FromBody] PartyListInput input, CancellationToken ct) =>
        service.UpdatePartyListAsync(electionId, partyListId, input, ct);

    [HttpDelete("{electionId:guid}/party-lists/{partyListId:guid}")]
    public async Task<IActionResult> DeletePartyList(Guid electionId, Guid partyListId, CancellationToken ct)
    {
        await service.DeletePartyListAsync(electionId, partyListId, ct);
        return NoContent();
    }

    [HttpPost("{electionId:guid}/party-lists/{partyListId:guid}/candidates")]
    public Task<CandidateView> AddCandidate(Guid electionId, Guid partyListId, [FromBody] CandidateInput input, CancellationToken ct) =>
        service.AddCandidateAsync(electionId, partyListId, input, ct);

    [HttpPut("{electionId:guid}/party-lists/{partyListId:guid}/candidates/{candidateId:guid}")]
    public Task<CandidateView> UpdateCandidate(Guid electionId, Guid partyListId, Guid candidateId,
        [FromBody] CandidateInput input, CancellationToken ct) =>
        service.UpdateCandidateAsync(electionId, partyListId, candidateId, input, ct);

    [HttpDelete("{electionId:guid}/party-lists/{partyListId:guid}/candidates/{candidateId:guid}")]
    public async Task<IActionResult> DeleteCandidate(Guid electionId, Guid partyListId, Guid candidateId, CancellationToken ct)
    {
        await service.DeleteCandidateAsync(electionId, partyListId, candidateId, ct);
        return NoContent();
    }

    [HttpPost("{electionId:guid}/party-lists/{partyListId:guid}/candidates/{candidateId:guid}/withdraw")]
    public async Task<IActionResult> WithdrawCandidate(Guid electionId, Guid partyListId, Guid candidateId, CancellationToken ct)
    {
        await service.WithdrawCandidateAsync(electionId, partyListId, candidateId, ct);
        return NoContent();
    }

    [HttpPut("{electionId:guid}/schedule")]
    public Task<AdminElectionView> UpdateSchedule(Guid electionId, [FromBody] ScheduleInput input, CancellationToken ct) =>
        service.UpdateScheduleAsync(electionId, input, ct);

    [HttpPost("{electionId:guid}/referendum-options")]
    public Task<ReferendumOptionView> AddReferendumOption(Guid electionId, [FromBody] ReferendumOptionInput input, CancellationToken ct) =>
        service.AddReferendumOptionAsync(electionId, input, ct);

    [HttpPut("{electionId:guid}/referendum-options/{optionId:guid}")]
    public Task<ReferendumOptionView> UpdateReferendumOption(Guid electionId, Guid optionId,
        [FromBody] ReferendumOptionInput input, CancellationToken ct) =>
        service.UpdateReferendumOptionAsync(electionId, optionId, input, ct);

    [HttpDelete("{electionId:guid}/referendum-options/{optionId:guid}")]
    public async Task<IActionResult> DeleteReferendumOption(Guid electionId, Guid optionId, CancellationToken ct)
    {
        await service.DeleteReferendumOptionAsync(electionId, optionId, ct);
        return NoContent();
    }

    [HttpGet("{electionId:guid}/voter-roll")]
    public Task<IReadOnlyList<VoterRollEntryView>> ListVoterRoll(Guid electionId, CancellationToken ct) =>
        service.ListVoterRollAsync(electionId, ct);

    [HttpPost("{electionId:guid}/voter-roll")]
    public async Task<IActionResult> AddVoter(Guid electionId, [FromBody] VoterRollInput input, CancellationToken ct)
    {
        await service.AddVoterAsync(electionId, input, ct);
        return NoContent();
    }

    [HttpPost("{electionId:guid}/voter-roll/bulk")]
    public Task<int> BulkAddVoters(Guid electionId, [FromBody] BulkVoterRollInput input, CancellationToken ct) =>
        service.BulkAddVotersAsync(electionId, input, ct);

    [HttpDelete("{electionId:guid}/voter-roll/{personId:guid}")]
    public async Task<IActionResult> RemoveVoter(Guid electionId, Guid personId, CancellationToken ct)
    {
        await service.RemoveVoterAsync(electionId, personId, ct);
        return NoContent();
    }

    [HttpGet("{electionId:guid}/invitations")]
    public Task<IReadOnlyList<InvitationAdminView>> ListInvitations(Guid electionId, CancellationToken ct) =>
        service.ListInvitationsAsync(electionId, ct);

    [HttpPost("{electionId:guid}/invitations")]
    public Task<InvitationCreated> CreateInvitation(Guid electionId, [FromBody] InvitationInput input, CancellationToken ct) =>
        service.CreateInvitationAsync(electionId, input, ct);

    [HttpPost("{electionId:guid}/invitations/bulk")]
    public Task<IReadOnlyList<InvitationCreated>> BulkCreateInvitations(Guid electionId,
        [FromBody] BulkInvitationInput input, CancellationToken ct) =>
        service.BulkCreateInvitationsAsync(electionId, input, ct);

    [HttpDelete("{electionId:guid}/invitations/{invitationId:guid}")]
    public Task<InvitationAdminView> RevokeInvitation(Guid electionId, Guid invitationId, CancellationToken ct) =>
        service.RevokeInvitationAsync(electionId, invitationId, ct);

    [HttpPost("{electionId:guid}/transitions")]
    public Task<AdminElectionView> Transition(Guid electionId, [FromBody] TransitionInput input, CancellationToken ct) =>
        service.TransitionAsync(electionId, input, ct);

    [HttpPut("{electionId:guid}/winners")]
    public Task<AdminElectionView> SetWinners(Guid electionId, [FromBody] WinnerSelectionInput input, CancellationToken ct) =>
        service.SetWinnersAsync(electionId, input, ct);

    [HttpPost("{electionId:guid}/force-certify")]
    public Task<AdminElectionView> ForceCertify(Guid electionId, CancellationToken ct) =>
        service.TransitionAsync(electionId, new TransitionInput(ElectionStatus.Certified, "Administrative certification override"), ct);
}