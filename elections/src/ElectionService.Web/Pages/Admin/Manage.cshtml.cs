using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public sealed class ManageModel(
    ManagedElectionClient managed,
    PublicElectionClient publicElections,
    IConfiguration configuration,
    IStringLocalizer localizer) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public string? Title { get; set; }
    [BindProperty(SupportsGet = true)] public string Step { get; set; } = "details";
    [BindProperty] public ElectionInput Election { get; set; } = new();
    [BindProperty] public PartyListInput PartyList { get; set; } = new();
    [BindProperty] public Guid EditPartyListId { get; set; }
    [BindProperty] public PartyListInput EditPartyList { get; set; } = new();
    [BindProperty] public Guid PartyListId { get; set; }
    [BindProperty] public CandidateInput Candidate { get; set; } = new();
    [BindProperty] public Guid EditCandidatePartyListId { get; set; }
    [BindProperty] public Guid EditCandidateId { get; set; }
    [BindProperty] public CandidateInput EditCandidate { get; set; } = new();
    [BindProperty] public ReferendumOptionInput ReferendumOption { get; set; } = new();
    [BindProperty] public Guid EditOptionId { get; set; }
    [BindProperty] public ReferendumOptionInput EditOption { get; set; } = new();
    [BindProperty] public VoterRollInput Voter { get; set; } = new();
    [BindProperty] public string BulkVoterPersonIds { get; set; } = string.Empty;
    [BindProperty] public InvitationInput Invitation { get; set; } = new();
    [BindProperty] public string BulkInvitationLines { get; set; } = string.Empty;
    [BindProperty] public TransitionInput Transition { get; set; } = new();
    [BindProperty] public bool ConfirmPublish { get; set; }
    [BindProperty] public IReadOnlyList<Guid> WinnerCandidateIds { get; set; } = [];

    public ElectionView? PublicElection { get; private set; }
    public InvitationCreated? CreatedInvitation { get; private set; }
    public IReadOnlyList<InvitationCreated> CreatedInvitations { get; private set; } = [];
    public IReadOnlyList<VoterRollEntryView> VoterRoll { get; private set; } = [];
    public IReadOnlyList<InvitationAdminView> Invitations { get; private set; } = [];
    public PartyListView? CreatedPartyList { get; private set; }
    public TabularResultsView? Results { get; private set; }
    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string ResultsCsvUrl => $"{(configuration["ElectionApi:PublicBaseUrl"]
        ?? configuration["ElectionApi:BaseUrl"]
        ?? "http://localhost:8085").TrimEnd('/')}/public/elections/{Id}/results.csv";

    public string? InviteUrl(string? token) =>
        string.IsNullOrEmpty(token) ? null : $"{Request.Scheme}://{Request.Host}/Invite/{Id}/{Uri.EscapeDataString(token)}";

    public async Task OnGetAsync(CancellationToken ct)
    {
        Step = NormalizeStep(Step);
        if (TempData["SuccessMessage"] is string message) SuccessMessage = message;
        await LoadPublicAsync(ct, populateForm: true);
    }

    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken ct) =>
        await ExecuteAsync(Election, nameof(Election), async () =>
        {
            var updated = await managed.UpdateAsync(Id, Election, ct);
            Title = updated.Title;
            SuccessMessage = localizer["Election details updated."];
        }, ct, "details");

    public async Task<IActionResult> OnPostPartyListAsync(CancellationToken ct) =>
        await ExecuteAsync(PartyList, nameof(PartyList), async () =>
        {
            CreatedPartyList = await managed.AddPartyListAsync(Id, PartyList, ct);
            SuccessMessage = localizer["Party list added. List ID: {0}", CreatedPartyList.Id];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostUpdatePartyListAsync(CancellationToken ct) =>
        await ExecuteAsync(EditPartyList, nameof(EditPartyList), async () =>
        {
            await managed.UpdatePartyListAsync(Id, EditPartyListId, EditPartyList, ct);
            SuccessMessage = localizer["Party list updated."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostDeletePartyListAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.DeletePartyListAsync(Id, EditPartyListId, ct);
            SuccessMessage = localizer["Party list and its draft candidates deleted."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostCandidateAsync(CancellationToken ct) =>
        await ExecuteAsync(Candidate, nameof(Candidate), async () =>
        {
            if (PartyListId == Guid.Empty) throw new InvalidOperationException(localizer["Party list is required."]);
            await managed.AddCandidateAsync(Id, PartyListId, Candidate, ct);
            SuccessMessage = localizer["Candidate added."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostUpdateCandidateAsync(CancellationToken ct) =>
        await ExecuteAsync(EditCandidate, nameof(EditCandidate), async () =>
        {
            await managed.UpdateCandidateAsync(Id, EditCandidatePartyListId, EditCandidateId, EditCandidate, ct);
            SuccessMessage = localizer["Candidate updated."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostDeleteCandidateAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.DeleteCandidateAsync(Id, EditCandidatePartyListId, EditCandidateId, ct);
            SuccessMessage = localizer["Candidate deleted."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostWithdrawCandidateAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.WithdrawCandidateAsync(Id, EditCandidatePartyListId, EditCandidateId, ct);
            SuccessMessage = localizer["Candidate withdrawn."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostEndNowAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            var election = await managed.GetAsync(Id, ct);
            await managed.UpdateScheduleAsync(Id, election.VotingStartsAt, DateTime.UtcNow, ct);
            SuccessMessage = localizer["Voting period ended."];
        }, ct, "details");

    public async Task<IActionResult> OnPostOptionAsync(CancellationToken ct) =>
        await ExecuteAsync(ReferendumOption, nameof(ReferendumOption), async () =>
        {
            await managed.AddOptionAsync(Id, ReferendumOption, ct);
            SuccessMessage = localizer["Referendum option added."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostUpdateOptionAsync(CancellationToken ct) =>
        await ExecuteAsync(EditOption, nameof(EditOption), async () =>
        {
            await managed.UpdateOptionAsync(Id, EditOptionId, EditOption, ct);
            SuccessMessage = localizer["Referendum option updated."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostDeleteOptionAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.DeleteOptionAsync(Id, EditOptionId, ct);
            SuccessMessage = localizer["Referendum option deleted."];
        }, ct, "ballot");

    public async Task<IActionResult> OnPostVoterAsync(CancellationToken ct) =>
        await ExecuteAsync(Voter, nameof(Voter), async () =>
        {
            await managed.AddVoterAsync(Id, Voter, ct);
            SuccessMessage = localizer["Voter added to the roll."];
        }, ct, "voters");

    public async Task<IActionResult> OnPostBulkVotersAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            var personIds = ParsePersonIds(BulkVoterPersonIds);
            var added = await managed.BulkAddVotersAsync(Id, new BulkVoterRollInput(personIds), ct);
            SuccessMessage = localizer["Added {0} new voter-roll entries.", added];
        }, ct, "voters");

    public async Task<IActionResult> OnPostRemoveVoterAsync(Guid personId, CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.RemoveVoterAsync(Id, personId, ct);
            SuccessMessage = localizer["Voter removed from the roll."];
        }, ct, "voters");

    public async Task<IActionResult> OnPostInvitationAsync(CancellationToken ct) =>
        await ExecuteAsync(Invitation, nameof(Invitation), async () =>
        {
            CreatedInvitation = await managed.CreateInvitationAsync(Id, Invitation, ct);
            SuccessMessage = localizer["Invitation created. Its token is shown below once."];
        }, ct, "invitations");

    public async Task<IActionResult> OnPostBulkInvitationsAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            var items = ParseInvitations(BulkInvitationLines);
            CreatedInvitations = await managed.BulkCreateInvitationsAsync(Id, new BulkInvitationInput(items), ct);
            SuccessMessage = localizer["Created {0} invitations. Their tokens are shown below once.", CreatedInvitations.Count];
        }, ct, "invitations");

    public async Task<IActionResult> OnPostRevokeInvitationAsync(Guid invitationId, CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.RevokeInvitationAsync(Id, invitationId, ct);
            SuccessMessage = localizer["Invitation revoked."];
        }, ct, "invitations");

    public async Task<IActionResult> OnPostTransitionAsync(CancellationToken ct) =>
        await ExecuteAsync(Transition, nameof(Transition), async () =>
        {
            if (Transition.Status == "Published" && !ConfirmPublish)
                throw new InvalidOperationException(localizer["Confirm publishing before changing the election state."]);
            var updated = await managed.TransitionAsync(Id, Transition, ct);
            Title = updated.Title;
            SuccessMessage = localizer["Election moved to {0}.", ElectionDisplay.Status(updated.Status, localizer)];
        }, ct, "publish");

    public async Task<IActionResult> OnPostForceCertifyAsync(CancellationToken ct) =>
        await ExecuteAsync(new { }, "ForceCertify", async () =>
        {
            var updated = await managed.ForceCertifyAsync(Id, ct);
            Title = updated.Title;
            SuccessMessage = localizer["Election certified by administrative override."];
        }, ct, "publish");

    public async Task<IActionResult> OnPostWinnersAsync(CancellationToken ct) =>
        await ExecuteActionAsync(async () =>
        {
            await managed.SetWinnersAsync(Id, new WinnerSelectionInput(WinnerCandidateIds), ct);
            SuccessMessage = localizer["Election winners updated."];
        }, ct, "results");

    private async Task<IActionResult> ExecuteAsync(
        object input,
        string prefix,
        Func<Task> action,
        CancellationToken ct,
        string step)
    {
        Step = step;
        ModelState.Clear();
        if (!TryValidateModel(input, prefix))
        {
            await LoadPublicAsync(ct, populateForm: false);
            return Page();
        }

        try
        {
            await action();
        }
        catch (ElectionApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
        }

        await LoadPublicAsync(ct, populateForm: false);
        return Page();
    }

    private async Task<IActionResult> ExecuteActionAsync(Func<Task> action, CancellationToken ct, string step)
    {
        Step = step;
        ModelState.Clear();
        try
        {
            await action();
        }
        catch (ElectionApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
        }
        await LoadPublicAsync(ct, populateForm: false);
        return Page();
    }

    private async Task LoadPublicAsync(CancellationToken ct, bool populateForm)
    {
        try
        {
            PublicElection = await managed.GetAsync(Id, ct);
            if (PublicElection.IsHistorical)
            {
                Title = PublicElection.Title;
                return;
            }

            var voterRollTask = managed.ListVoterRollAsync(Id, ct);
            var invitationsTask = managed.ListInvitationsAsync(Id, ct);
            await Task.WhenAll(voterRollTask, invitationsTask);
            VoterRoll = await voterRollTask;
            Invitations = await invitationsTask;
            if (PublicElection.Status is "Published" or "Finalized" or "Certified" or "Archived")
            {
                Results = await publicElections.TabularResultsAsync(Id, ct);
            }
            Title = PublicElection.Title;
            if (!populateForm) return;
            Election = new ElectionInput
            {
                Slug = PublicElection.Slug,
                Title = PublicElection.Title,
                Description = PublicElection.Description,
                Type = PublicElection.Type,
                EligibilityMode = PublicElection.EligibilityMode,
                VotingStartsAt = ElectionInput.UtcToPrague(PublicElection.VotingStartsAt),
                VotingEndsAt = ElectionInput.UtcToPrague(PublicElection.VotingEndsAt),
                TerritoryCode = PublicElection.TerritoryCode,
                EligibleVoterCount = null,
                SeatCount = PublicElection.SeatCount
            };
        }
        catch (HttpRequestException)
        {
            ErrorMessage ??= localizer["The election administration record is temporarily unavailable."];
        }
        catch (ElectionApiException)
        {
            ErrorMessage ??= localizer["The election administration record is temporarily unavailable."];
        }
    }

    private IReadOnlyList<Guid> ParsePersonIds(string input)
    {
        var personIds = new List<Guid>();
        foreach (var line in Lines(input))
        {
            if (!Guid.TryParse(line, out var personId))
                throw new InvalidOperationException(localizer["Invalid person GUID: {0}", line]);
            personIds.Add(personId);
        }
        if (personIds.Count == 0) throw new InvalidOperationException(localizer["Enter at least one person GUID."]);
        return personIds;
    }

    private IReadOnlyList<InvitationInput> ParseInvitations(string input)
    {
        var items = new List<InvitationInput>();
        foreach (var line in Lines(input))
        {
            var parts = line.Split('|', 2);
            Guid? personId = null;
            if (parts[0].Length != 0)
            {
                if (!Guid.TryParse(parts[0], out var parsed))
                    throw new InvalidOperationException(localizer["Invalid invitation person GUID: {0}", parts[0]]);
                personId = parsed;
            }
            items.Add(new InvitationInput { PersonId = personId, Label = parts.Length == 2 ? parts[1] : null });
        }
        if (items.Count == 0) throw new InvalidOperationException(localizer["Enter at least one invitation."]);
        if (items.Count > 500) throw new InvalidOperationException(localizer["A maximum of 500 invitations can be created at once."]);
        return items;
    }

    private static IEnumerable<string> Lines(string input) => input
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(line => line.Length != 0);

    private static string NormalizeStep(string? step) => step is "details" or "ballot" or "voters" or "invitations" or "publish" or "results"
        ? step
        : "details";
}