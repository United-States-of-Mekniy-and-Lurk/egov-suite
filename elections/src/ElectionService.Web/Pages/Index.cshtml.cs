using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages;

public sealed class IndexModel(PublicElectionClient elections) : PageModel
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
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            IsUnavailable = true;
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }
}