using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages.Elections;

public sealed class CalendarModel(PublicElectionClient elections) : PageModel
{
    public IReadOnlyList<ElectionView> Elections { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        try { Elections = await elections.ListAsync(ct); }
        catch (HttpRequestException) { }
    }
}
