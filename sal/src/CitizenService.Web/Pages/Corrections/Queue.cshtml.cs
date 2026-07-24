using System.Net.Http.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Corrections;

[Authorize(Policy = "RequireClerk")]
public class CorrectionsQueueModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public List<FieldCorrectionRequestViewModel> Requests { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public CorrectionsQueueModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/correction-requests?status=Submitted&take=100", ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "corrections.load_failed";
            return;
        }
        Requests = await response.Content.ReadFromJsonAsync<List<FieldCorrectionRequestViewModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<IActionResult> OnPostReviewAsync(
        Guid id,
        string decision,
        string reason,
        CancellationToken ct)
    {
        if (decision is not ("approve" or "reject"))
            return BadRequest();

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.PostAsJsonAsync(
            $"/correction-requests/{id}/{decision}",
            new { reason },
            ct);
        if (response.IsSuccessStatusCode)
            return RedirectToPage();

        ErrorMessage = "corrections.review_failed";
        await OnGetAsync(ct);
        return Page();
    }
}