using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Manage;

[Authorize]
public sealed class CorrectionsModel(ManagedRegistryClient registry) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid OrganizationId { get; set; }
    [BindProperty, Required] public string FieldKey { get; set; } = string.Empty;
    [BindProperty] public string? ProposedValue { get; set; }
    [BindProperty, Required, StringLength(1000)] public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<CorrectionRequest> Requests { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        try
        {
            await registry.CreateCorrectionAsync(OrganizationId, FieldKey, ProposedValue, Reason, ct);
            return RedirectToPage(new { OrganizationId });
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
        try { Requests = await registry.ListCorrectionsAsync(OrganizationId, ct); }
        catch (HttpRequestException exception) { ErrorMessage = exception.Message; }
    }
}