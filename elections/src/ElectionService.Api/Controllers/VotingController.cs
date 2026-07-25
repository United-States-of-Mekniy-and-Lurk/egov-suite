using ElectionService.Application.Models;
using ElectionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
public sealed class VotingController(VotingService voting, PublicElectionService elections) : ControllerBase
{
    [Authorize]
    [HttpPost("elections/{electionId:guid}/vote")]
    public Task<BallotReceipt> CitizenVote(Guid electionId, [FromBody] VoteInput input, CancellationToken ct) =>
        voting.VoteAsCitizenAsync(electionId, input, ct);

    [AllowAnonymous]
    [HttpGet("invitations/{electionId:guid}/{token}")]
    public Task<InvitationDetail> Invitation(Guid electionId, string token, CancellationToken ct) =>
        elections.InvitationAsync(electionId, token, ct);

    [AllowAnonymous]
    [HttpPost("invitations/{electionId:guid}/{token}/vote")]
    public Task<BallotReceipt> InvitationVote(Guid electionId, string token, [FromBody] VoteInput input, CancellationToken ct) =>
        voting.VoteByInvitationAsync(electionId, token, input, ct);
}