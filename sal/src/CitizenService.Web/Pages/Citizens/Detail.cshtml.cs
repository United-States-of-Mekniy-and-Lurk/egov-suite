using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Citizens;

[Authorize(Policy = "RequireClerk")]
public class CitizenDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CitizenViewModel? Citizen { get; set; }

    public CitizenDetailModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizens/{id}", ct);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            Citizen = JsonSerializer.Deserialize<CitizenViewModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
