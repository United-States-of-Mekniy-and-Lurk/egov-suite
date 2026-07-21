using System.Net.Http.Json;
using System.Text.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class ApplicationsIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentPersonService _currentPersonService;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PersonViewModel? Person { get; set; }
    public List<ApplicationViewModel> Applications { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public ApplicationsIndexModel(
        IHttpClientFactory httpClientFactory,
        CurrentPersonService currentPersonService)
    {
        _httpClientFactory = httpClientFactory;
        _currentPersonService = currentPersonService;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Resolve current user
        Person = await _currentPersonService.GetAsync(ct);
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

    public async Task<IActionResult> OnPostAbandonAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.PostAsJsonAsync(
            $"/citizenship-applications/{id}/transition",
            new { targetState = "Withdrawn", reason = "Abandoned by applicant" },
            ct);
        if (response.IsSuccessStatusCode)
            return RedirectToPage();

        ErrorMessage = "The application could not be abandoned.";
        await OnGetAsync(ct);
        return Page();
    }
}
