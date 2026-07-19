using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Citizens;

[Authorize(Policy = "RequireClerk")]
public class CitizensIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public List<CitizenViewModel> Citizens { get; set; } = new();

    public CitizensIndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/citizens?skip=0&take=50", ct);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            Citizens = JsonSerializer.Deserialize<List<CitizenViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
    }
}
