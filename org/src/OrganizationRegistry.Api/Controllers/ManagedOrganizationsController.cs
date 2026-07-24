using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[Authorize]
[Route("organizations")]
public sealed class ManagedOrganizationsController(
    OrganizationQueryService organizations,
    CorrectionService corrections) : ControllerBase
{
    [HttpGet("mine")]
    public Task<IReadOnlyList<ManagedOrganizationView>> Mine(CancellationToken ct) => organizations.ListMineAsync(ct);

    [HttpGet("{organizationId:guid}/correction-requests")]
    public Task<IReadOnlyList<CorrectionView>> Corrections(Guid organizationId, CancellationToken ct) =>
        corrections.ListAsync(organizationId, ct);

    [HttpPost("{organizationId:guid}/correction-requests")]
    public async Task<ActionResult<CorrectionView>> RequestCorrection(
        Guid organizationId,
        CreateCorrectionInput input,
        CancellationToken ct)
    {
        var correction = await corrections.CreateAsync(organizationId, input, ct);
        return Created($"/organizations/{organizationId}/correction-requests/{correction.Id}", correction);
    }
}