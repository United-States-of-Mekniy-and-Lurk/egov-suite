using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Staff;

[Authorize(Policy = "RequireClerk")]
public sealed class OrganizationsModel(ManagedRegistryClient registry) : PageModel
{
    public IReadOnlyList<StaffOrganization> Organizations { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try { Organizations = await registry.ListStaffOrganizationsAsync(ct); }
        catch (HttpRequestException ex) { ErrorMessage = ex.Message; }
    }

    public async Task<IActionResult> OnPostUpdateDatesAsync(Guid organizationId, DateOnly? establishedOn, CancellationToken ct)
    {
        try
        {
            await registry.UpdateOrganizationDatesAsync(organizationId, establishedOn, ct);
            SuccessMessage = "Updated.";
        }
        catch (HttpRequestException ex) { ErrorMessage = ex.Message; }

        await OnGetAsync(ct);
        return Page();
    }
}
