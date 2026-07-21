using System.Net.Http.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Corrections;

[Authorize]
public class NewCorrectionModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentPersonService _currentPersonService;

    [BindProperty(SupportsGet = true)]
    public string FieldKey { get; set; } = string.Empty;

    [BindProperty]
    public string? ProposedValue { get; set; }

    [BindProperty]
    public string RequestReason { get; set; } = string.Empty;

    public CitizenRegistryFieldViewModel? Field { get; private set; }
    public string? ErrorMessage { get; private set; }

    public NewCorrectionModel(
        IHttpClientFactory httpClientFactory,
        CurrentPersonService currentPersonService)
    {
        _httpClientFactory = httpClientFactory;
        _currentPersonService = currentPersonService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Page() : RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty)
            return RedirectToPage("/Index");

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.PostAsJsonAsync(
            $"/citizens/{person.Id}/fields/{Uri.EscapeDataString(FieldKey)}/correction-requests",
            new { proposedValue = ProposedValue, requestReason = RequestReason },
            ct);
        if (response.IsSuccessStatusCode)
            return RedirectToPage("/Corrections/Index");

        ErrorMessage = response.StatusCode == System.Net.HttpStatusCode.Conflict
            ? "A correction request for this field is already awaiting review."
            : "The correction request could not be submitted. Check the proposed value and reason.";
        await LoadAsync(ct);
        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        var person = await _currentPersonService.GetAsync(ct);
        if (person == null || person.Id == Guid.Empty || string.IsNullOrWhiteSpace(FieldKey))
            return false;

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var fields = await client.GetFromJsonAsync<List<CitizenRegistryFieldViewModel>>(
            $"/citizens/{person.Id}/fields", ct) ?? [];
        Field = fields.FirstOrDefault(field =>
            field.Definition.Key == FieldKey && field.Definition.UserEditable);
        return Field != null;
    }
}