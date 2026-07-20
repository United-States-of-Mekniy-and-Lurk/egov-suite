using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    [BindProperty]
    public string FormSubmissionJson { get; set; } = "{}";

    public string FormDefinitionJson { get; set; } = "{}";
    public string FormTitle { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public NewApplicationModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var loaded = await LoadLatestFormAsync(ct);
        return loaded ? Page() : RedirectToPage("/Index");
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
        var formResponse = await client.GetAsync("/forms/citizenship_application/latest", ct);
        if (!formResponse.IsSuccessStatusCode)
            return RedirectToPage("/Index");

        var form = await formResponse.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null)
            return RedirectToPage("/Index");

        SetFormPresentation(form);
        JsonDocument answers;
        try
        {
            answers = JsonDocument.Parse(FormSubmissionJson);
            if (answers.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException();
        }
        catch (JsonException)
        {
            ErrorMessage = "The submitted form data is invalid.";
            return Page();
        }

        using (answers)
        {
            var result = await SaveApplicationAsync(client, person.Id, form, answers, ct);
            if (result != null) return result;
        }

        return RedirectToPage("/Index");
    }

    private async Task<IActionResult?> SaveApplicationAsync(
        HttpClient client,
        Guid personId,
        ApplicationFormViewModel form,
        JsonDocument answers,
        CancellationToken ct)
    {

        var applicationsResponse = await client.GetAsync(
            $"/citizenship-applications?personId={personId}", ct);
        var applications = applicationsResponse.IsSuccessStatusCode
            ? await applicationsResponse.Content.ReadFromJsonAsync<List<ApplicationViewModel>>(JsonOptions, ct) ?? []
            : [];
        var application = applications.FirstOrDefault(item => item.Status == "Draft");

        if (application == null)
        {
            var createBody = new { personId, formName = form.Name, formVersion = form.Version };
            var createResponse = await client.PostAsJsonAsync("/citizenship-applications", createBody, ct);
            if (!createResponse.IsSuccessStatusCode)
            {
                ErrorMessage = "The application could not be created.";
                return Page();
            }

            application = await createResponse.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
            if (application == null)
                return RedirectToPage("/Index");
        }

        var answersResponse = await client.PutAsJsonAsync(
            $"/citizenship-applications/{application.Id}/answers",
            new { answers = answers.RootElement }, ct);
        if (!answersResponse.IsSuccessStatusCode)
        {
            ErrorMessage = "The application answers could not be saved.";
            return Page();
        }

        var transitionResponse = await client.PostAsJsonAsync(
            $"/citizenship-applications/{application.Id}/transition",
            new { targetState = 1, reason = (string?)null }, ct);
        if (!transitionResponse.IsSuccessStatusCode)
        {
            ErrorMessage = await transitionResponse.Content.ReadAsStringAsync(ct);
            return Page();
        }

        return null;
    }

    private async Task<bool> LoadLatestFormAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/forms/citizenship_application/latest", ct);
        if (!response.IsSuccessStatusCode)
            return false;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null)
            return false;

        SetFormPresentation(form);
        return true;
    }

    private void SetFormPresentation(ApplicationFormViewModel form)
    {
        var node = JsonNode.Parse(form.DefinitionJson) as JsonObject;
        if (node?["components"] is JsonArray)
        {
            FormDefinitionJson = node.ToJsonString(JsonOptions);
            FormTitle = GetLocalizedTitle(node);
            return;
        }

        var legacy = JsonSerializer.Deserialize<ApplicationFormDefinition>(form.DefinitionJson, JsonOptions)
            ?? new ApplicationFormDefinition();
        var converted = FormioDefinitionAdapter.ConvertLegacy(legacy, JsonOptions);
        FormDefinitionJson = converted.ToJsonString(JsonOptions);
        FormTitle = GetLocalizedTitle(converted);
    }

    private static string GetLocalizedTitle(JsonObject definition)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return definition["properties"]?["titles"]?[culture]?.GetValue<string>()
            ?? definition["properties"]?["titles"]?["en"]?.GetValue<string>()
            ?? definition["title"]?.GetValue<string>()
            ?? string.Empty;
    }
}
