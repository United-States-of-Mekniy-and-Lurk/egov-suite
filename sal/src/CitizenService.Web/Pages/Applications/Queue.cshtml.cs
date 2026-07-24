using System.Net.Http.Json;
using CitizenService.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Applications;

[Authorize(Policy = "RequireClerk")]
public class ApplicationQueueModel : PageModel
{
    private static readonly string[] PendingStatuses = ["Submitted", "UnderReview"];
    private static readonly HashSet<string> AllowedStatuses =
        ["Pending", "Submitted", "UnderReview", "Approved", "Rejected", "Withdrawn", "All"];
    private readonly IHttpClientFactory _httpClientFactory;

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "Pending";

    public List<ApplicationViewModel> Applications { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public ApplicationQueueModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!AllowedStatuses.Contains(Status))
            Status = "Pending";

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var statuses = Status switch
        {
            "Pending" => PendingStatuses,
            "All" => [string.Empty],
            _ => [Status]
        };

        foreach (var status in statuses)
        {
            var query = string.IsNullOrEmpty(status)
                ? "/citizenship-applications?take=100"
                : $"/citizenship-applications?status={status}&take=100";
            var response = await client.GetAsync(query, ct);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "applications.queue.load_failed";
                Applications.Clear();
                return;
            }

            Applications.AddRange(
                await response.Content.ReadFromJsonAsync<List<ApplicationViewModel>>(cancellationToken: ct) ?? []);
        }

        Applications = Applications
            .OrderBy(application => application.Status == "Submitted" ? 0 : 1)
            .ThenBy(application => application.SubmittedAt ?? application.CreatedAt)
            .ToList();
    }
}