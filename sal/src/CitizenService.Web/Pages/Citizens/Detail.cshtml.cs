using System.Net.Http.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Citizens;

[Authorize(Policy = "RequireClerk")]
public class CitizenDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CitizenViewModel? Citizen { get; set; }
    public List<CitizenRegistryFieldViewModel> RegistryFields { get; set; } = [];
    public List<CitizenRegistryFieldHistoryViewModel> FieldHistory { get; set; } = [];

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
            Citizen = await response.Content.ReadFromJsonAsync<CitizenViewModel>(cancellationToken: ct);
            if (Citizen == null) return;
            RegistryFields = await client.GetFromJsonAsync<List<CitizenRegistryFieldViewModel>>(
                $"/citizens/{Citizen.PersonId}/fields", ct) ?? [];
            FieldHistory = await client.GetFromJsonAsync<List<CitizenRegistryFieldHistoryViewModel>>(
                $"/citizens/{Citizen.PersonId}/field-history", ct) ?? [];
        }
    }
}
