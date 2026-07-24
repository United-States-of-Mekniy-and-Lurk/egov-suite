using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Staff;

[Authorize(Policy = "RequireClerk")]
public sealed class HistoricalRegistrationModel(
    ManagedRegistryClient managedRegistry,
    PublicRegistryClient publicRegistry) : PageModel
{
    [BindProperty, Required, StringLength(32)] public string RegistrationNumber { get; set; } = string.Empty;
    [BindProperty, Required] public DateOnly? RegisteredOn { get; set; }
    [BindProperty] public DateOnly? EstablishedOn { get; set; }
    [BindProperty, Required, StringLength(240)] public string SourceReference { get; set; } = string.Empty;
    [BindProperty, StringLength(2000)] public string? ImportNote { get; set; }
    [BindProperty] public Guid? OwnerPersonId { get; set; }
    [BindProperty, Required, StringLength(240)] public string LegalName { get; set; } = string.Empty;
    [BindProperty, StringLength(240)] public string? TradingName { get; set; }
    [BindProperty, Required, StringLength(80)] public string LegalFormCode { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(2000)] public string Purpose { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(500)] public string RegisteredAddress { get; set; } = string.Empty;
    [BindProperty] public string[] ClassificationCodes { get; set; } = [];
    public IReadOnlyList<Classification> Classifications { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadClassificationsAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadClassificationsAsync(ct);
        if (!ModelState.IsValid || !RegisteredOn.HasValue) return Page();

        try
        {
            var organization = await managedRegistry.CreateHistoricalOrganizationAsync(
                new HistoricalOrganizationInput(
                    RegistrationNumber,
                    RegisteredOn.Value,
                    EstablishedOn,
                    SourceReference,
                    ImportNote,
                    OwnerPersonId,
                    LegalName,
                    TradingName,
                    LegalFormCode,
                    Purpose,
                    RegisteredAddress,
                    ClassificationCodes),
                ct);
            return RedirectToPage("/Organizations/Detail", new { identifier = organization.Slug });
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    private async Task LoadClassificationsAsync(CancellationToken ct)
    {
        try { Classifications = await publicRegistry.ListClassificationsAsync(ct); }
        catch (HttpRequestException) { Classifications = []; }
    }
}