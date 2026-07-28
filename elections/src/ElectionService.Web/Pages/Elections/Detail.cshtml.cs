using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Pages.Elections;

public sealed class DetailModel(
    PublicElectionClient publicElections,
    ManagedElectionClient managedElections,
    IStringLocalizer localizer) : PageModel
{
    public ElectionView? Election { get; private set; }
    public OfficialElectionRecordView? OfficialRecord { get; private set; }
    public BallotReceipt? Receipt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsUnavailable { get; private set; }

    [BindProperty]
    public VoteInput Vote { get; set; } = new();

    public async Task OnGetAsync(string identifier, CancellationToken ct) => await LoadAsync(identifier, ct);

    public async Task<IActionResult> OnPostAsync(string identifier, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = Url.Page("/Elections/Detail", new { identifier })
            });

        await LoadAsync(identifier, ct);
        if (Election is null) return Page();
        if (!Election.IsOpen)
        {
            ErrorMessage = localizer["Voting is not currently open for this election."];
            return Page();
        }
        if (!ModelState.IsValid) return Page();

        try
        {
            Receipt = await managedElections.VoteAsync(Election.Id, Vote, ct);
        }
        catch (ElectionApiException)
        {
            ErrorMessage = localizer["Your vote could not be submitted. Please check the ballot and try again."];
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
        }
        return Page();
    }

    private async Task LoadAsync(string identifier, CancellationToken ct)
    {
        try
        {
            Election = await publicElections.GetAsync(identifier, ct);
            if (Election is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (Election.HasPublicResults)
            {
                OfficialRecord = await publicElections.RecordAsync(identifier, ct);
                Election = OfficialRecord.Election;
            }
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