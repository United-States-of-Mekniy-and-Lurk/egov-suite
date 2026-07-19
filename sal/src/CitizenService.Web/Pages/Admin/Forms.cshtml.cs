using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public class FormsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer _localizer;

    [BindProperty]
    public string FormName { get; set; } = "citizenship_application";

    [BindProperty]
    public string DefinitionJson { get; set; } = """
        {
          "title": "Citizenship Application",
          "fields": [
            { "name": "legal_name", "type": "text", "label": "Legal Name", "required": true },
            { "name": "date_of_birth", "type": "date", "label": "Date of Birth", "required": true },
            { "name": "motivation", "type": "textarea", "label": "Motivation", "required": true }
          ]
        }
        """;

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public FormsModel(IHttpClientFactory httpClientFactory, IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.PostAsJsonAsync(
            $"/forms/{Uri.EscapeDataString(FormName)}",
            new { definitionJson = DefinitionJson }, ct);

        IsError = !response.IsSuccessStatusCode;
        Message = response.IsSuccessStatusCode
            ? _localizer["admin.forms.created"]
            : await response.Content.ReadAsStringAsync(ct);
        return Page();
    }
}
