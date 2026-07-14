using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class ApplicationsIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public List<ApplicationViewModel> Applications { get; set; } = new();

    public ApplicationsIndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/citizenship-applications?skip=0&take=50", ct);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            Applications = JsonSerializer.Deserialize<List<ApplicationViewModel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
    }
}
