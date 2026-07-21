using System.Net.Http.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Corrections;

[Authorize]
public class CorrectionsIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentPersonService _currentPersonService;

    public List<FieldCorrectionRequestViewModel> Requests { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public CorrectionsIndexModel(
        IHttpClientFactory httpClientFactory,
        CurrentPersonService currentPersonService)
    {
        _httpClientFactory = httpClientFactory;
        _currentPersonService = currentPersonService;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty)
            return;

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizens/{person.Id}/correction-requests", ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = "Correction requests could not be loaded.";
            return;
        }

        Requests = await response.Content.ReadFromJsonAsync<List<FieldCorrectionRequestViewModel>>(cancellationToken: ct) ?? [];
    }
}