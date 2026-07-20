using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Applications;

[Authorize]
public class NewApplicationModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentPersonService _currentPersonService;
    private readonly IStringLocalizer _localizer;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [BindProperty]
    public string FormSubmissionJson { get; set; } = "{}";

    [BindProperty(SupportsGet = true)]
    public Guid? ApplicationId { get; set; }

    public string FormDefinitionJson { get; set; } = "{}";
    public string FormTitle { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime? DraftUpdatedAt { get; set; }

    public NewApplicationModel(
        IHttpClientFactory httpClientFactory,
        CurrentPersonService currentPersonService,
        IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _currentPersonService = currentPersonService;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty)
            return RedirectToPage("/Index");

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var draft = await GetDraftAsync(client, person.Id, ApplicationId, ct);
        var loaded = draft == null
            ? await LoadLatestFormAsync(client, ct)
            : await LoadDraftAsync(client, draft, ct);
        return loaded ? Page() : RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        => await SaveAsync(submit: true, ct);

    public async Task<IActionResult> OnPostSaveDraftAsync(CancellationToken ct)
        => await SaveAsync(submit: false, ct);

    public async Task<IActionResult> OnPostAutosaveAsync(CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty)
            return Unauthorized();

        JsonDocument answers;
        try
        {
            answers = JsonDocument.Parse(FormSubmissionJson);
            if (answers.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException();
        }
        catch (JsonException)
        {
            return BadRequest(new { error = _localizer["applications.invalid_data"].Value });
        }

        using (answers)
        {
            var client = _httpClientFactory.CreateClient("CitizenApi");
            var draft = await GetDraftAsync(client, person.Id, ApplicationId, ct);
            if (draft == null)
            {
                var formResponse = await client.GetAsync("/forms/citizenship_application/latest", ct);
                if (!formResponse.IsSuccessStatusCode)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);

                var form = await formResponse.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
                if (form == null)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);

                var createResponse = await client.PostAsJsonAsync(
                    "/citizenship-applications",
                    new { personId = person.Id, formName = form.Name, formVersion = form.Version },
                    ct);
                if (!createResponse.IsSuccessStatusCode)
                    return StatusCode((int)createResponse.StatusCode);

                draft = await createResponse.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
                if (draft == null)
                    return StatusCode(StatusCodes.Status502BadGateway);
            }

            var answersResponse = await client.PutAsJsonAsync(
                $"/citizenship-applications/{draft.Id}/answers",
                new { answers = answers.RootElement },
                ct);
            if (!answersResponse.IsSuccessStatusCode)
                return StatusCode((int)answersResponse.StatusCode);

            var saved = await answersResponse.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
            return new JsonResult(new
            {
                applicationId = draft.Id,
                updatedAt = saved?.UpdatedAt ?? DateTime.UtcNow
            });
        }
    }

    private async Task<IActionResult> SaveAsync(bool submit, CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty)
            return RedirectToPage("/Index");

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var draft = await GetDraftAsync(client, person.Id, ApplicationId, ct);
        var formPath = draft == null
            ? "/forms/citizenship_application/latest"
            : $"/forms/{draft.FormName}/{draft.FormVersion}";
        var formResponse = await client.GetAsync(formPath, ct);
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
            var result = await SaveApplicationAsync(client, person.Id, form, draft, answers, submit, ct);
            if (result != null) return result;
        }

        if (!submit)
        {
            StatusMessage = _localizer["applications.draft_saved"];
            DraftUpdatedAt = DateTime.UtcNow;
            return Page();
        }

        return RedirectToPage("/Index");
    }

    private async Task<IActionResult?> SaveApplicationAsync(
        HttpClient client,
        Guid personId,
        ApplicationFormViewModel form,
        ApplicationViewModel? application,
        JsonDocument answers,
        bool submit,
        CancellationToken ct)
    {
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
            ApplicationId = application.Id;
        }

        var answersResponse = await client.PutAsJsonAsync(
            $"/citizenship-applications/{application.Id}/answers",
            new { answers = answers.RootElement }, ct);
        if (!answersResponse.IsSuccessStatusCode)
        {
            ErrorMessage = "The application answers could not be saved.";
            return Page();
        }

        if (!submit)
            return null;

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

    private async Task<bool> LoadLatestFormAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("/forms/citizenship_application/latest", ct);
        if (!response.IsSuccessStatusCode)
            return false;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null)
            return false;

        SetFormPresentation(form);
        return true;
    }

    private async Task<bool> LoadDraftAsync(
        HttpClient client,
        ApplicationViewModel draft,
        CancellationToken ct)
    {
        var response = await client.GetAsync($"/forms/{draft.FormName}/{draft.FormVersion}", ct);
        if (!response.IsSuccessStatusCode)
            return false;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null)
            return false;

        SetFormPresentation(form);
        FormSubmissionJson = draft.FormAnswers?.GetRawText() ?? "{}";
        DraftUpdatedAt = draft.UpdatedAt;
        return true;
    }

    private static async Task<ApplicationViewModel?> GetDraftAsync(
        HttpClient client,
        Guid personId,
        Guid? applicationId,
        CancellationToken ct)
    {
        if (applicationId.HasValue)
        {
            var applicationResponse = await client.GetAsync(
                $"/citizenship-applications/{applicationId.Value}", ct);
            if (!applicationResponse.IsSuccessStatusCode)
                return null;

            var selected = await applicationResponse.Content.ReadFromJsonAsync<ApplicationViewModel>(JsonOptions, ct);
            return selected is { Status: "Draft" } && selected.PersonId == personId
                ? selected
                : null;
        }

        var response = await client.GetAsync(
            $"/citizenship-applications?personId={personId}", ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var applications = await response.Content.ReadFromJsonAsync<List<ApplicationViewModel>>(JsonOptions, ct) ?? [];
        return applications
            .Where(application => application.Status == "Draft")
            .OrderByDescending(application => application.UpdatedAt)
            .FirstOrDefault();
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
