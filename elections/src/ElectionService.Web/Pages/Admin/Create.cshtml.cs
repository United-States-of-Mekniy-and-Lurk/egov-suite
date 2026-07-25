using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public sealed class CreateModel(ManagedElectionClient elections, IStringLocalizer<SharedResource> localizer) : PageModel
{
    [BindProperty]
    public ElectionInput Input { get; set; } = new()
    {
        VotingStartsAt = DateTimeOffset.UtcNow.AddDays(7),
        VotingEndsAt = DateTimeOffset.UtcNow.AddDays(8)
    };

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            var election = await elections.CreateAsync(Input, ct);
            return RedirectToPage("/Admin/Manage", new { id = election.Id, title = election.Title });
        }
        catch (ElectionApiException)
        {
            ErrorMessage = localizer["The election could not be created. Check the entered data and try again."];
            return Page();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
            return Page();
        }
    }
}