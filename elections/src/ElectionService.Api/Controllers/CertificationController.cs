using ElectionService.Application.Models;
using ElectionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
[Authorize]
[Route("elections/{electionId:guid}/certification")]
public sealed class CertificationController(CertificationService certification) : ControllerBase
{
    [HttpPost]
    public Task<CertificationView> Certify(Guid electionId, [FromBody] CertificationInput input, CancellationToken ct) =>
        certification.CertifyAsync(electionId, input, ct);
}
