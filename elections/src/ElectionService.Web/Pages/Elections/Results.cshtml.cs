using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages.Elections;

public sealed class ResultsModel(PublicElectionClient elections) : PageModel
{
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string Identifier { get; set; } = string.Empty;

    public TabularResultsView? Results { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var election = await elections.GetAsync(Identifier, ct);
        if (election is null) return;
        try { Results = await elections.TabularResultsAsync(election.Id, ct); }
        catch (HttpRequestException) { }
    }
}
