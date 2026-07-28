using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages.Elections;

public sealed class VerifyModel(PublicElectionClient elections) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid ElectionId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Receipt { get; set; }
    public ReceiptVerificationResult? Result { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Receipt)) return;
        try { Result = await elections.VerifyReceiptAsync(ElectionId, Receipt.Trim(), ct); }
        catch (HttpRequestException) { }
    }
}
