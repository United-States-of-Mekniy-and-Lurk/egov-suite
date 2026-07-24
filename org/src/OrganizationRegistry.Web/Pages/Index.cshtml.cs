using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages;

public class IndexModel(PublicRegistryClient registry) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Classification { get; set; }

    public IReadOnlyList<PublicOrganization> Organizations { get; private set; } = [];
    public IReadOnlyList<Classification> Classifications { get; private set; } = [];
    public bool IsUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            var classificationsTask = registry.ListClassificationsAsync(ct);
            var organizationsTask = registry.ListOrganizationsAsync(Search, Classification, ct);
            await Task.WhenAll(classificationsTask, organizationsTask);
            Classifications = await classificationsTask;
            Organizations = await organizationsTask;
        }
        catch (HttpRequestException)
        {
            IsUnavailable = true;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            IsUnavailable = true;
        }
    }
}
