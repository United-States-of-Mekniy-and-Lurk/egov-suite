using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.Organizations;

public sealed class DetailModel(PublicRegistryClient registry) : PageModel
{
    public PublicOrganization? Organization { get; private set; }
    public bool IsUnavailable { get; private set; }

    public async Task OnGetAsync(string identifier, CancellationToken ct)
    {
        try
        {
            Organization = await registry.GetOrganizationAsync(identifier, ct);
            if (Organization is null) Response.StatusCode = StatusCodes.Status404NotFound;
        }
        catch (HttpRequestException)
        {
            IsUnavailable = true;
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            IsUnavailable = true;
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }
}