using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Abstractions;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireClerk")]
[Route("staff/organizations")]
public sealed class StaffOrganizationsController(IOrganizationRegistryStore store) : ControllerBase
{
    public sealed record UpdateOrganizationDatesInput(DateOnly? EstablishedOn);
    public sealed record StaffOrganizationView(Guid Id, string LegalName, string RegistrationNumber, DateOnly? EstablishedOn);

    [HttpGet]
    public async Task<IReadOnlyList<StaffOrganizationView>> List(CancellationToken ct)
    {
        var organizations = await store.ListPublicOrganizationsAsync(null, null, 0, 1000, ct);
        return organizations.Select(o => new StaffOrganizationView(o.Id, o.LegalName, o.RegistrationNumber, o.EstablishedOn)).ToList();
    }

    [HttpPatch("{id:guid}/dates")]
    public async Task<IActionResult> UpdateDates(Guid id, UpdateOrganizationDatesInput input, CancellationToken ct)
    {
        var organization = await store.GetOrganizationAsync(id, ct);
        if (organization is null) return NotFound();

        organization.EstablishedOn = input.EstablishedOn;
        await store.SaveChangesAsync(ct);
        return NoContent();
    }
}
