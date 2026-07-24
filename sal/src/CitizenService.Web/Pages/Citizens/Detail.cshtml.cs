using System.Net.Http.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Citizens;

[Authorize(Policy = "RequireClerk")]
public class CitizenDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PersonDirectoryService _personDirectory;

    public CitizenViewModel? Citizen { get; set; }
    public PersonViewModel? Person { get; set; }
    public List<CitizenRegistryFieldViewModel> RegistryFields { get; set; } = [];
    public List<CitizenRegistryFieldHistoryViewModel> FieldHistory { get; set; } = [];

    public CitizenDetailModel(
        IHttpClientFactory httpClientFactory,
        PersonDirectoryService personDirectory)
    {
        _httpClientFactory = httpClientFactory;
        _personDirectory = personDirectory;
    }

    public async Task OnGetAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizens/{id}", ct);
        if (response.IsSuccessStatusCode)
        {
            Citizen = await response.Content.ReadFromJsonAsync<CitizenViewModel>(cancellationToken: ct);
            if (Citizen == null) return;
            Person = await _personDirectory.GetAsync(Citizen.PersonId, ct);
            RegistryFields = await client.GetFromJsonAsync<List<CitizenRegistryFieldViewModel>>(
                $"/citizens/{Citizen.PersonId}/fields", ct) ?? [];
            FieldHistory = await client.GetFromJsonAsync<List<CitizenRegistryFieldHistoryViewModel>>(
                $"/citizens/{Citizen.PersonId}/field-history", ct) ?? [];
        }
    }
}
