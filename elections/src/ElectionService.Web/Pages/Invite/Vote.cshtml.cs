using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Pages.Invite;

public sealed class VoteModel(InvitationElectionClient invitations, IStringLocalizer localizer) : PageModel
{
    public ElectionView? Election { get; private set; }
    public InvitationDetail? Invitation { get; private set; }
    public BallotReceipt? Receipt { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public VoteInput Vote { get; set; } = new();

    public async Task OnGetAsync(Guid electionId, string token, CancellationToken ct)
    {
        SetPrivateHeaders();
        await LoadAsync(electionId, token, ct);
    }

    public async Task OnPostAsync(Guid electionId, string token, CancellationToken ct)
    {
        SetPrivateHeaders();
        await LoadAsync(electionId, token, ct);
        if (Election is null || Invitation?.IsAvailable != true || !ModelState.IsValid) return;

        try
        {
            Receipt = await invitations.VoteAsync(electionId, token, Vote, ct);
        }
        catch (ElectionApiException)
        {
            ErrorMessage = localizer["Your vote could not be submitted. Please check the ballot and try again."];
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
        }
    }

    private async Task LoadAsync(Guid electionId, string token, CancellationToken ct)
    {
        try
        {
            Invitation = await invitations.GetInvitationAsync(electionId, token, ct);
            Election = await invitations.GetElectionAsync(electionId, ct);
        }
        catch (ElectionApiException exception)
        {
            ErrorMessage = exception.StatusCode == System.Net.HttpStatusCode.NotFound
                ? localizer["This invitation is invalid or no longer available."]
                : localizer["The invitation could not be loaded. Please try again."];
            Response.StatusCode = (int)exception.StatusCode;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }

    private void SetPrivateHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
}