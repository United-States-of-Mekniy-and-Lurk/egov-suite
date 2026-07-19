using System.Net.Http.Json;
using System.Text.Json;
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
        public List<string> SelectedFieldKeys { get; set; } = [];

        public List<RegistryFieldDefinitionViewModel> Definitions { get; set; } = [];
        public List<ApplicationFormViewModel> Forms { get; set; } = [];
        public List<string> UnavailableFieldKeys { get; set; } = [];

    public string? Message { get; set; }
    public bool IsError { get; set; }

    public FormsModel(IHttpClientFactory httpClientFactory, IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadWorkspaceAsync(ct);
        await LoadLatestDefinitionAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        await LoadWorkspaceAsync(ct);

        var selected = Definitions
            .Where(definition => definition.IsActive && SelectedFieldKeys.Contains(definition.Key))
            .OrderBy(definition => definition.SortOrder)
            .ToList();
        if (selected.Count == 0)
        {
            IsError = true;
            Message = _localizer["admin.forms.fields_required"];
            return Page();
        }

        if (selected.Any(definition =>
                definition.FieldType == "Select" && definition.OptionSourceType != "Static"))
        {
            IsError = true;
            Message = _localizer["admin.forms.remote_select_unsupported"];
            return Page();
        }

        var titles = new Dictionary<string, string> { ["en"] = TitleEn.Trim() };
        if (!string.IsNullOrWhiteSpace(TitleCs)) titles["cs"] = TitleCs.Trim();
        var definition = new ApplicationFormDefinition
        {
            Title = TitleEn.Trim(),
            Titles = titles,
            Fields = selected.Select(ToFormField).ToList()
        };
        var definitionJson = JsonSerializer.Serialize(definition, JsonOptions);
        var response = await client.PostAsJsonAsync(
            $"/forms/{Uri.EscapeDataString(FormName)}",
            new { definitionJson }, ct);

        IsError = !response.IsSuccessStatusCode;
        Message = response.IsSuccessStatusCode
            ? _localizer["admin.forms.created"]
            : await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode)
            await LoadWorkspaceAsync(ct);
        return Page();
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

    private async Task LoadLatestDefinitionAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync($"/forms/{Uri.EscapeDataString(FormName)}/latest", ct);
        if (!response.IsSuccessStatusCode) return;

        var form = await response.Content.ReadFromJsonAsync<ApplicationFormViewModel>(JsonOptions, ct);
        if (form == null) return;

        var definition = JsonSerializer.Deserialize<ApplicationFormDefinition>(form.DefinitionJson, JsonOptions);
        if (definition == null) return;

        TitleEn = definition.Titles.GetValueOrDefault("en", definition.Title);
        TitleCs = definition.Titles.GetValueOrDefault("cs", string.Empty);
        SelectedFieldKeys = definition.Fields.Select(field => field.Name).ToList();
        var registryKeys = Definitions.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        UnavailableFieldKeys = SelectedFieldKeys.Where(key => !registryKeys.Contains(key)).ToList();
    }

    private static ApplicationFormField ToFormField(RegistryFieldDefinitionViewModel definition)
        => new()
        {
            Name = definition.Key,
            Type = definition.FieldType switch
            {
                "MultilineText" => "textarea",
                "Date" => "date",
                "Integer" or "Decimal" => "number",
                "Boolean" => "checkbox",
                "Select" => "select",
                _ => "text"
            },
            Step = definition.FieldType switch
            {
                "Integer" => "1",
                "Decimal" => "any",
                _ => null
            },
            Label = definition.GetLabel("en"),
            Labels = definition.Labels,
            Required = definition.IsRequired,
            Options = definition.StaticOptions?.Select(option => new ApplicationFormFieldOption
            {
                Value = option.Value,
                Labels = option.Labels
            }).ToList() ?? []
        };
}
