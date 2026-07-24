using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Staff;

[Authorize(Policy = "RequireClerk")]
public sealed class RegistrationsModel(ManagedRegistryClient registry) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    public IReadOnlyList<RegistrationApplication> Applications { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostTransitionAsync(Guid id, string targetStatus, string? reason, CancellationToken ct)
    {
        try
        {
            await registry.TransitionAsync(id, targetStatus, reason, ct);
            return RedirectToPage(new { Status });
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.Message;
            await LoadAsync(ct);
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try { Applications = await registry.ListQueueAsync(Status, ct); }
        catch (HttpRequestException exception) { ErrorMessage = exception.Message; }
    }
}