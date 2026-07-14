using System.Net.Http.Json;
using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class ApplicationDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApplicationViewModel? Application { get; set; }

    public ApplicationDetailModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizenship-applications/{id}", ct);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            Application = JsonSerializer.Deserialize<ApplicationViewModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string targetState, string? reason, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var body = new { targetState, reason };
        await client.PostAsJsonAsync($"/citizenship-applications/{id}/transition", body, ct);
        return RedirectToPage(new { id });
    }
}
