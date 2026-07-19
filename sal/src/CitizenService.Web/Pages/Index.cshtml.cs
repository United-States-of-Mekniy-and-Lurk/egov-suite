using System.Text.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IndexModel> _logger;

    public PersonViewModel? Person { get; set; }
    public CitizenViewModel? Citizen { get; set; }
    public string UserState { get; set; } = "unknown"; // "citizen", "new", "error"
    public string? ErrorMessage { get; set; }

    public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Step 1: Resolve the current user via Ego Person Registry
        var egoClient = _httpClientFactory.CreateClient("PersonRegistry");
        try
        {
            var meResponse = await egoClient.GetAsync("/me", ct);
            if (!meResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ego /me returned {StatusCode}", meResponse.StatusCode);
                UserState = "error";
                ErrorMessage = "Could not resolve your identity. The Person Registry may be unavailable.";
                return;
            }

            var meContent = await meResponse.Content.ReadAsStringAsync(ct);
            Person = JsonSerializer.Deserialize<PersonViewModel>(meContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (Person == null || Person.Id == Guid.Empty)
            {
                UserState = "error";
                ErrorMessage = "Your identity could not be resolved.";
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Ego Person Registry");
            UserState = "error";
            ErrorMessage = "Could not connect to the Person Registry.";
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
                Citizen = JsonSerializer.Deserialize<CitizenViewModel>(citizenContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                UserState = "citizen";
                return;
            }

            // 404 = not a citizen yet
            UserState = "new";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check citizen status");
            UserState = "error";
            ErrorMessage = "Could not verify your citizen status.";
        }
    }
}

