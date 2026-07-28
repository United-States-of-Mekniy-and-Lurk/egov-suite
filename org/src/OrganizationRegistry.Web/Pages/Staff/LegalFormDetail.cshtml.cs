using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Staff;

[Authorize(Policy = "RequireAdmin")]
public sealed class LegalFormDetailModel(ManagedRegistryClient registry) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Code { get; set; } = string.Empty;
    public LegalForm? LegalForm { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostUpdateDescriptionAsync(string? descriptionEn, string? descriptionCs, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (LegalForm is null) return Page();

        try
        {
            var input = new UpdateLegalFormInput(
                LegalForm.LabelEn,
                LegalForm.LabelCs,
                LegalForm.IsActive,
                LegalForm.SortOrder,
                descriptionEn,
                descriptionCs);
            await registry.UpdateLegalFormAsync(Code, input, ct);
            SuccessMessage = "Description updated.";
            await LoadAsync(ct);
        }
        catch (HttpRequestException ex) { ErrorMessage = ex.Message; }

        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try { LegalForm = await registry.GetLegalFormAsync(Code, ct); }
        catch (HttpRequestException ex) { ErrorMessage = ex.Message; }
    }
}
