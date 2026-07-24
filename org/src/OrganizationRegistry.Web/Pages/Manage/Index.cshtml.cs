using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Manage;

[Authorize]
public sealed class IndexModel(ManagedRegistryClient registry) : PageModel
{
    public IReadOnlyList<ManagedOrganization> Organizations { get; private set; } = [];
    public IReadOnlyList<RegistrationApplication> Applications { get; private set; } = [];
    public bool IsUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            var organizationsTask = registry.ListOrganizationsAsync(ct);
            var applicationsTask = registry.ListApplicationsAsync(ct);
            await Task.WhenAll(organizationsTask, applicationsTask);
            Organizations = await organizationsTask;
            Applications = await applicationsTask;
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