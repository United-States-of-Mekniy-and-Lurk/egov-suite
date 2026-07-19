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

    public ApplicationFormDefinition FormDefinition { get; set; } = new();
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

        var definition = JsonSerializer.Deserialize<ApplicationFormDefinition>(form.DefinitionJson, JsonOptions)
            ?? new ApplicationFormDefinition();
        var answers = definition.Fields.ToDictionary(
            field => field.Name,
            field => Request.Form[field.Name].ToString());

        var missingRequired = definition.Fields.Any(field =>
            field.Required && string.IsNullOrWhiteSpace(answers[field.Name]));
        if (missingRequired)
        {
            FormDefinition = definition;
            ErrorMessage = "Please complete all required fields.";
            return Page();
        }

        var applicationsResponse = await client.GetAsync(
            $"/citizenship-applications?personId={person.Id}", ct);
        var applications = applicationsResponse.IsSuccessStatusCode
            ? await applicationsResponse.Content.ReadFromJsonAsync<List<ApplicationViewModel>>(JsonOptions, ct) ?? []
            : [];
        var application = applications.FirstOrDefault(item => item.Status == "Draft");

        if (application == null)
        {
            var createBody = new { personId = person.Id, formName = form.Name, formVersion = form.Version };
            var createResponse = await client.PostAsJsonAsync("/citizenship-applications", createBody, ct);
            if (!createResponse.IsSuccessStatusCode)
            {
                FormDefinition = definition;
                ErrorMessage = "The application could not be created.";
                return Page();
            }

            application = await createResponse.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
            if (application == null)
                return RedirectToPage("/Index");
        }

        await client.PutAsJsonAsync(
            $"/citizenship-applications/{application.Id}/answers",
            new { answers }, ct);
        await client.PostAsJsonAsync(
            $"/citizenship-applications/{application.Id}/transition",
            new { targetState = 1, reason = (string?)null }, ct);

        return RedirectToPage("/Index");
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

        FormDefinition = JsonSerializer.Deserialize<ApplicationFormDefinition>(form.DefinitionJson, JsonOptions)
            ?? new ApplicationFormDefinition();
        return true;
    }
}
