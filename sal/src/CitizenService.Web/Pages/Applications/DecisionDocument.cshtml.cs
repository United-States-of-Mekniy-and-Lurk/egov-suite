using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class DecisionDocumentModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DecisionDocumentModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizenship-applications/{id}/decision-document", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();
        if (!response.IsSuccessStatusCode)
            return RedirectToPage("/Applications/Index");

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"citizenship-decision-{id:N}.pdf";
        return File(content, "application/pdf", fileName);
    }
}