using ElectionService.Application.Models;
using ElectionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route("admin/historical-elections")]
public sealed class AdminHistoricalElectionsController(AdminElectionService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AdminElectionView>> Import(
        [FromBody] HistoricalElectionInput input, CancellationToken ct)
    {
        var election = await service.ImportHistoricalAsync(input, ct);
        return Created($"/public/elections/{election.Slug}/record", election);
    }
}