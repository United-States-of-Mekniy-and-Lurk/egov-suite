using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public class FormsModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer _localizer;

    [BindProperty]
    public string FormName { get; set; } = "citizenship_application";

    [BindProperty]
    public string TitleEn { get; set; } = "Citizenship Application";

    [BindProperty]
    public string TitleCs { get; set; } = string.Empty;

    [BindProperty]
    public string FormDefinitionJson { get; set; } = """
        {"title":"Citizenship Application","display":"form","components":[]}
        """;

    public List<RegistryFieldDefinitionViewModel> Definitions { get; set; } = [];
    public List<ApplicationFormViewModel> Forms { get; set; } = [];

    public string? Message { get; set; }
    public bool IsError { get; set; }
    public DateTime? DraftUpdatedAt { get; set; }

    public FormsModel(IHttpClientFactory httpClientFactory, IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadWorkspaceAsync(ct);
        await LoadDefinitionAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await SaveDraftAsync(ct);
        if (result != null) return result;

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var name = Uri.EscapeDataString(FormName.Trim());
        var response = await client.PostAsync($"/forms/{name}/draft/publish", null, ct);
        IsError = !response.IsSuccessStatusCode;
        Message = response.IsSuccessStatusCode
            ? _localizer["admin.forms.created"]
            : await response.Content.ReadAsStringAsync(ct);
        await LoadWorkspaceAsync(ct);
        if (response.IsSuccessStatusCode)
            DraftUpdatedAt = null;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync(CancellationToken ct)
    {
        var result = await SaveDraftAsync(ct);
        if (result != null) return result;

        Message = _localizer["admin.forms.draft_saved"];
        DraftUpdatedAt = DateTime.UtcNow;
        return Page();
    }

    private async Task<IActionResult?> SaveDraftAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        await LoadWorkspaceAsync(ct);

        JsonObject definition;
        try
        {
            definition = JsonNode.Parse(FormDefinitionJson) as JsonObject
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            IsError = true;
            Message = _localizer["admin.forms.invalid_definition"];
            return Page();
        }

        var titles = new Dictionary<string, string> { ["en"] = TitleEn.Trim() };
        if (!string.IsNullOrWhiteSpace(TitleCs)) titles["cs"] = TitleCs.Trim();
        definition["title"] = TitleEn.Trim();
        var properties = definition["properties"] as JsonObject ?? new JsonObject();
        properties["titles"] = JsonSerializer.SerializeToNode(titles, JsonOptions);
        definition["properties"] = properties;
        var definitionJson = definition.ToJsonString(JsonOptions);
        var response = await client.PutAsJsonAsync(
            $"/forms/{Uri.EscapeDataString(FormName.Trim())}/draft",
            new { definitionJson }, ct);

        IsError = !response.IsSuccessStatusCode;
        if (!response.IsSuccessStatusCode)
        {
            Message = await response.Content.ReadAsStringAsync(ct);
            return Page();
        }

        FormDefinitionJson = definitionJson;
        return null;
    }

    private async Task LoadWorkspaceAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var definitionsResponse = await client.GetAsync("/registry-fields?includeInactive=true", ct);
        if (definitionsResponse.IsSuccessStatusCode)
        {
            Definitions = await definitionsResponse.Content.ReadFromJsonAsync<List<RegistryFieldDefinitionViewModel>>(
                JsonOptions, ct) ?? [];
        }

        var formsResponse = await client.GetAsync("/forms", ct);
        if (formsResponse.IsSuccessStatusCode)
        {
            Forms = await formsResponse.Content.ReadFromJsonAsync<List<ApplicationFormViewModel>>(
                JsonOptions, ct) ?? [];
        }
    }

    private async Task LoadDefinitionAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var name = Uri.EscapeDataString(FormName);
        var response = await client.GetAsync($"/forms/{name}/draft", ct);
        if (response.IsSuccessStatusCode)
        {
            var draft = await response.Content.ReadFromJsonAsync<ApplicationFormDraftViewModel>(JsonOptions, ct);
            if (draft != null)
            {
                DraftUpdatedAt = draft.UpdatedAt;
                SetDefinition(draft.DefinitionJson);
                return;
            }
        }

        response = await client.GetAsync($"/forms/{name}/latest", ct);
        if (!response.IsSuccessStatusCode) return;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null) return;

        SetDefinition(form.DefinitionJson);
    }

    private void SetDefinition(string definitionJson)
    {
        var node = JsonNode.Parse(definitionJson) as JsonObject;
        if (node?["components"] is JsonArray)
        {
            FormDefinitionJson = node.ToJsonString(JsonOptions);
            TitleEn = node["properties"]?["titles"]?["en"]?.GetValue<string>()
                ?? node["title"]?.GetValue<string>()
                ?? TitleEn;
            TitleCs = node["properties"]?["titles"]?["cs"]?.GetValue<string>() ?? string.Empty;
            return;
        }

        var legacy = JsonSerializer.Deserialize<ApplicationFormDefinition>(definitionJson, JsonOptions);
        if (legacy == null) return;

        TitleEn = legacy.Titles.GetValueOrDefault("en", legacy.Title);
        TitleCs = legacy.Titles.GetValueOrDefault("cs", string.Empty);
        FormDefinitionJson = FormioDefinitionAdapter.ConvertLegacy(legacy, JsonOptions)
            .ToJsonString(JsonOptions);
    }
}
