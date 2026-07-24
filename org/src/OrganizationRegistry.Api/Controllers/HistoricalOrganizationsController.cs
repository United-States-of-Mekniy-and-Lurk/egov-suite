using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireClerk")]
[Route("staff/historical-organizations")]
public sealed class HistoricalOrganizationsController(HistoricalOrganizationService organizations) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PublicOrganizationView>> Create(
        CreateHistoricalOrganizationInput input,
        CancellationToken ct)
    {
        var organization = await organizations.CreateAsync(input, ct);
        return Created($"/public/organizations/{organization.Slug}", organization);
    }
}