using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Manage;

[Authorize]
public sealed class RegisterModel(ManagedRegistryClient managedRegistry, PublicRegistryClient publicRegistry) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty, Required, StringLength(200)] public string LegalName { get; set; } = string.Empty;
    [BindProperty, StringLength(200)] public string? TradingName { get; set; }
    [BindProperty, Required, StringLength(50)] public string LegalFormCode { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(2000)] public string Purpose { get; set; } = string.Empty;
    [BindProperty, Required, StringLength(500)] public string RegisteredAddress { get; set; } = string.Empty;
    [BindProperty] public string[] ClassificationCodes { get; set; } = [];
    public IReadOnlyList<Classification> Classifications { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadClassificationsAsync(ct);
        if (!Id.HasValue) return Page();

        try
        {
            var application = await managedRegistry.GetApplicationAsync(Id.Value, ct);
            if (application is null) return NotFound();
            LegalName = application.LegalName;
            TradingName = application.TradingName;
            LegalFormCode = application.LegalFormCode;
            Purpose = application.Purpose;
            RegisteredAddress = application.RegisteredAddress;
            ClassificationCodes = application.ClassificationCodes.ToArray();
            return Page();
        }
        catch (HttpRequestException)
        {
            return RedirectToPage("/Manage/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(string action, CancellationToken ct)
    {
        await LoadClassificationsAsync(ct);
        if (!ModelState.IsValid) return Page();
        try
        {
            var input = new RegistrationApplicationInput(
                LegalName, TradingName, LegalFormCode, Purpose, RegisteredAddress, ClassificationCodes);
            var application = Id.HasValue
                ? await managedRegistry.UpdateApplicationAsync(Id.Value, input, ct)
                : await managedRegistry.CreateApplicationAsync(input, ct);
            if (action == "submit")
                await managedRegistry.TransitionAsync(application.Id, "Submitted", null, ct);
            return RedirectToPage("/Manage/Index");
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