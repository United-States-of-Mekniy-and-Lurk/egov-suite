using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class NewApplicationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NewApplicationModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(Guid personId, string formName, int formVersion, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var body = new { personId, formName, formVersion };
        var response = await client.PostAsJsonAsync("/citizenship-applications", body, ct);
        if (response.IsSuccessStatusCode)
            return RedirectToPage("Index");
        return Page();
    }
}
