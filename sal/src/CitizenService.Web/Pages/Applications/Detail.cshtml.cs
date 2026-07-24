using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Applications;

[Authorize(Policy = "RequireClerk")]
public class ApplicationDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer _localizer;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApplicationViewModel? Application { get; set; }
    public string FormDefinitionJson { get; set; } = "{}";
    public string FormSubmissionJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }

    public ApplicationDetailModel(IHttpClientFactory httpClientFactory, IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/citizenship-applications/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();
        if (!response.IsSuccessStatusCode)
            return RedirectToPage("/Applications/Queue");

        Application = await response.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
        if (Application == null)
            return NotFound();

        FormSubmissionJson = Application.FormAnswers?.GetRawText() ?? "{}";
        await LoadFormDefinitionAsync(client, Application, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string targetState, string? reason, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var body = new { targetState, reason };
        var response = await client.PostAsJsonAsync($"/citizenship-applications/{id}/transition", body, ct);
        if (!response.IsSuccessStatusCode)
        {
            ErrorMessage = _localizer["applications.transition_failed"].Value;
            await OnGetAsync(id, ct);
            return Page();
        }

        return targetState is "Approved" or "Rejected"
            ? RedirectToPage("/Applications/Queue")
            : RedirectToPage(new { id });
    }

    private async Task LoadFormDefinitionAsync(
        HttpClient client,
        ApplicationViewModel application,
        CancellationToken ct)
    {
        var response = await client.GetAsync($"/forms/{application.FormName}/{application.FormVersion}", ct);
        if (!response.IsSuccessStatusCode)
            return;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null)
            return;

        var node = JsonNode.Parse(form.DefinitionJson) as JsonObject;
        if (node?["components"] is JsonArray)
        {
            FormDefinitionJson = node.ToJsonString(JsonOptions);
            return;
        }

        var legacy = JsonSerializer.Deserialize<ApplicationFormDefinition>(form.DefinitionJson, JsonOptions)
            ?? new ApplicationFormDefinition();
        FormDefinitionJson = FormioDefinitionAdapter.ConvertLegacy(legacy, JsonOptions).ToJsonString(JsonOptions);
    }
}
