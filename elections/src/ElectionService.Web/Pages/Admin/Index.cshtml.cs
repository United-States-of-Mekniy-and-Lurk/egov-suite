using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public sealed class IndexModel(ManagedElectionClient elections) : PageModel
{
    public IReadOnlyList<ElectionView> Elections { get; private set; } = [];
    public bool IsUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            Elections = await elections.ListAsync(ct);
        }
        catch (HttpRequestException)
        {
            IsUnavailable = true;
        }
    }
}