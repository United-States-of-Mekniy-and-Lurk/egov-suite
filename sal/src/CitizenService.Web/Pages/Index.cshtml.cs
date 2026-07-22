using System.Text.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CurrentPersonService _currentPersonService;
    private readonly ILogger<IndexModel> _logger;

    public PersonViewModel? Person { get; set; }
    public CitizenViewModel? Citizen { get; set; }
    public List<CitizenRegistryFieldViewModel> RegistryFields { get; set; } = [];
    public ApplicationViewModel? PendingApplication { get; set; }
    public string UserState { get; set; } = "unknown"; // "citizen", "pending", "new", "error"
    public string? ErrorMessage { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IndexModel(
        IHttpClientFactory httpClientFactory,
        CurrentPersonService currentPersonService,
        ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _currentPersonService = currentPersonService;
        _logger = logger;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Step 1: Resolve the current user via Ego Person Registry
        try
        {
            Person = await _currentPersonService.GetAsync(ct);

            if (Person == null || Person.Id == Guid.Empty)
            {
                UserState = "error";
                ErrorMessage = "We could not finish setting up your account. Please try again.";
                return;
            }
        }
        catch (DownstreamUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Ego Person Registry");
            UserState = "error";
            ErrorMessage = "We could not finish setting up your account. Please try again.";
            return;
        }

        // Step 2: Check if the person is already a citizen
        var citizenClient = _httpClientFactory.CreateClient("CitizenApi");
        try
        {
            var citizenResponse = await citizenClient.GetAsync($"/citizens/{Person.Id}", ct);
            if (citizenResponse.IsSuccessStatusCode)
            {
                var citizenContent = await citizenResponse.Content.ReadAsStringAsync(ct);
                Citizen = JsonSerializer.Deserialize<CitizenViewModel>(citizenContent, JsonOptions);
                UserState = "citizen";

                var fieldsResponse = await citizenClient.GetAsync($"/citizens/{Person.Id}/fields", ct);
                if (fieldsResponse.IsSuccessStatusCode)
                {
                    var fieldsContent = await fieldsResponse.Content.ReadAsStringAsync(ct);
                    RegistryFields = JsonSerializer.Deserialize<List<CitizenRegistryFieldViewModel>>(
                        fieldsContent, JsonOptions) ?? [];
                }
                return;
            }
        }
        catch (DownstreamUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check citizen status");
            UserState = "error";
            ErrorMessage = "Could not verify your citizen status.";
            return;
        }

        // Step 3: Check for an existing application
        try
        {
            var appsResponse = await citizenClient.GetAsync(
                $"/citizenship-applications?personId={Person.Id}", ct);
            if (appsResponse.IsSuccessStatusCode)
            {
                var appsContent = await appsResponse.Content.ReadAsStringAsync(ct);
                var apps = JsonSerializer.Deserialize<List<ApplicationViewModel>>(appsContent, JsonOptions) ?? [];
                PendingApplication = apps.FirstOrDefault(a =>
                    a.Status is "Draft" or "Submitted" or "UnderReview");
            }
        }
        catch (DownstreamUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check existing applications");
        }

        UserState = PendingApplication != null ? "pending" : "new";
    }

    public async Task<IActionResult> OnPostApplyAsync(CancellationToken ct)
    {
        return RedirectToPage("/Applications/New");
    }
}

