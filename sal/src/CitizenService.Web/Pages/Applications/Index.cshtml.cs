using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class ApplicationsIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PersonViewModel? Person { get; set; }
    public List<ApplicationViewModel> Applications { get; set; } = new();

    public ApplicationsIndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Resolve current user
        var egoClient = _httpClientFactory.CreateClient("PersonRegistry");
        var meResponse = await egoClient.GetAsync("/me", ct);
        if (!meResponse.IsSuccessStatusCode) return;

        var meContent = await meResponse.Content.ReadAsStringAsync(ct);
        Person = JsonSerializer.Deserialize<PersonViewModel>(meContent, JsonOptions);
        if (Person == null || Person.Id == Guid.Empty) return;

        // Fetch only this user's applications
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizenship-applications?personId={Person.Id}", ct);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            Applications = JsonSerializer.Deserialize<List<ApplicationViewModel>>(content, JsonOptions) ?? new();
        }
    }
}
