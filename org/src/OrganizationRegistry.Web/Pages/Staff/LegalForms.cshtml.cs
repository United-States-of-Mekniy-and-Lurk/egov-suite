using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Staff;

[Authorize(Policy = "RequireAdmin")]
public sealed class LegalFormsModel(ManagedRegistryClient registry) : PageModel
{
    public IReadOnlyList<LegalForm> LegalForms { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostCreateAsync(
        string code,
        string labelEn,
        string labelCs,
        int sortOrder,
        CancellationToken ct)
    {
        try
        {
            await registry.CreateLegalFormAsync(new CreateLegalFormInput(code, labelEn, labelCs, sortOrder), ct);
            return RedirectToPage();
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = exception.Message;
            await LoadAsync(ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        string code,
        string labelEn,
        string labelCs,
        bool isActive,
        int sortOrder,
        CancellationToken ct)
    {
        try
        {
            await registry.UpdateLegalFormAsync(
                code,
                new UpdateLegalFormInput(labelEn, labelCs, isActive, sortOrder),
                ct);
            return RedirectToPage();
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
        try { LegalForms = await registry.ListLegalFormsAsync(ct); }
        catch (HttpRequestException exception) { ErrorMessage = exception.Message; }
    }
}