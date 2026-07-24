using GovernmentPortal.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GovernmentPortal.Web.Pages;

[Authorize]
public sealed class OverviewModel(IEnumerable<IPortalModule> modules) : PageModel
{
    public IReadOnlyList<PortalModuleView> Modules { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Modules = await Task.WhenAll(modules.Select(module => module.GetAsync(cancellationToken)));
    }
}