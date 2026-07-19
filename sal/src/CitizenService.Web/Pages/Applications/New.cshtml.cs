using System.Net.Http.Json;
using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class NewApplicationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NewApplicationModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult OnGet()
    {
        // Direct navigation is not supported — apply from the home page
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        // Resolve the current user's PersonId from Ego
        var egoClient = _httpClientFactory.CreateClient("PersonRegistry");
        var meResponse = await egoClient.GetAsync("/me", ct);
        if (!meResponse.IsSuccessStatusCode)
            return RedirectToPage("/Index");

        var meContent = await meResponse.Content.ReadAsStringAsync(ct);
        var person = JsonSerializer.Deserialize<PersonViewModel>(meContent, JsonOptions);
        if (person == null || person.Id == Guid.Empty)
            return RedirectToPage("/Index");

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var body = new { personId = person.Id, formName = "citizenship_application", formVersion = 1 };
        await client.PostAsJsonAsync("/citizenship-applications", body, ct);
        return RedirectToPage("/Index");
    }
}
