using System.ComponentModel.DataAnnotations;
using ElectionService.Web.Models;
using ElectionService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public sealed class HistoricalImportModel(
    ManagedElectionClient elections,
    IStringLocalizer<SharedResource> localizer) : PageModel
{
    [BindProperty] public HistoricalImportForm Input { get; set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        IReadOnlyList<HistoricalPartyListInput>? partyLists = null;
        IReadOnlyList<HistoricalReferendumOptionInput>? referendumOptions = null;

        if (Input.VotingEndsAt <= Input.VotingStartsAt)
            ModelState.AddModelError("Input.VotingEndsAt", localizer["Voting end must be after voting start."]);
        if (Input.EligibleVoterCount.HasValue && Input.ParticipatingVoterCount > Input.EligibleVoterCount.Value)
            ModelState.AddModelError("Input.ParticipatingVoterCount", localizer["Participating voters cannot exceed eligible voters."]);

        if (Input.Type == "PartyList")
            partyLists = ParsePartyLists(Input.PartyListLines, Input.CandidateLines);
        else if (Input.Type == "Referendum")
            referendumOptions = ParseReferendumOptions(Input.ReferendumLines);
        else
            ModelState.AddModelError("Input.Type", localizer["Select a supported election type."]);

        var validBallotCount = partyLists?.Sum(item => (long)item.VoteCount) ??
                               referendumOptions?.Sum(item => (long)item.VoteCount) ?? 0;
        if (validBallotCount + Input.InvalidBallotCount > Input.ParticipatingVoterCount)
            ModelState.AddModelError("Input.ParticipatingVoterCount",
                localizer["Valid and invalid ballots cannot exceed participating voters."]);

        if (!ModelState.IsValid) return Page();

        var payload = new HistoricalElectionInput(
            Input.Slug, Input.Title, Input.Description, Input.Type,
            Input.VotingStartsAt, Input.VotingEndsAt, EmptyToNull(Input.TerritoryCode),
            Input.SourceReference, Input.EligibleVoterCount, Input.ParticipatingVoterCount,
            Input.InvalidBallotCount, partyLists, referendumOptions);

        try
        {
            var election = await elections.ImportHistoricalAsync(payload, ct);
            TempData["SuccessMessage"] = localizer["Historical election record imported."];
            return RedirectToPage("/Admin/Manage", new { id = election.Id, title = election.Title });
        }
        catch (ElectionApiException)
        {
            ErrorMessage = localizer["The historical record could not be imported. Check the entered data and try again."];
        }
        catch (HttpRequestException)
        {
            ErrorMessage = localizer["The election service is temporarily unavailable."];
        }

        return Page();
    }

    private IReadOnlyList<HistoricalPartyListInput> ParsePartyLists(string input, string candidateInput)
    {
        var parties = new List<(int Line, HistoricalPartyListInput Value)>();
        foreach (var (line, number) in Lines(input))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length is not 5 and not 6)
            {
                AddLineError("Input.PartyListLines", "Party-list line {0}: expected 5 or 6 fields.", number);
                continue;
            }
            if (!TryInt(parts[0], "Input.PartyListLines", "Party-list line {0}: sort order must be a whole number.", number, out var sort) |
                !TryInt(parts[4], "Input.PartyListLines", "Party-list line {0}: votes must be a non-negative whole number.", number, out var votes, nonNegative: true)) continue;
            Guid? organizationId = null;
            if (parts.Length == 6 && parts[5].Length > 0)
            {
                if (!Guid.TryParse(parts[5], out var parsedOrganizationId))
                {
                    AddLineError("Input.PartyListLines", "Party-list line {0}: organization ID must be a GUID or empty.", number);
                    continue;
                }
                organizationId = parsedOrganizationId;
            }
            if (parts[1].Length == 0 || parts[2].Length == 0 || parts[3].Length == 0)
            {
                AddLineError("Input.PartyListLines", "Party-list line {0}: registration number, party name, and list name are required.", number);
                continue;
            }
            parties.Add((number, new HistoricalPartyListInput(organizationId, parts[1], parts[2], parts[3], sort, [], votes)));
        }

        foreach (var duplicate in parties.GroupBy(item => item.Value.SortOrder).Where(group => group.Count() > 1).SelectMany(group => group.Skip(1)))
            AddLineError("Input.PartyListLines", "Party-list line {0}: sort order is duplicated.", duplicate.Line);
        foreach (var duplicate in parties.Where(item => item.Value.PartyOrganizationId.HasValue)
                     .GroupBy(item => item.Value.PartyOrganizationId).Where(group => group.Count() > 1).SelectMany(group => group.Skip(1)))
            AddLineError("Input.PartyListLines", "Party-list line {0}: organization ID is duplicated.", duplicate.Line);

        var candidatesBySort = new Dictionary<int, List<HistoricalCandidateInput>>();
        foreach (var (line, number) in Lines(candidateInput))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length is < 3 or > 5)
            {
                AddLineError("Input.CandidateLines", "Candidate line {0}: expected 3 to 5 fields.", number);
                continue;
            }
            if (!TryInt(parts[0], "Input.CandidateLines", "Candidate line {0}: party sort order must be a whole number.", number, out var partySort) |
                !TryInt(parts[1], "Input.CandidateLines", "Candidate line {0}: position must be a positive whole number.", number, out var position, positive: true)) continue;
            if (parts[2].Length == 0)
            {
                AddLineError("Input.CandidateLines", "Candidate line {0}: display name is required.", number);
                continue;
            }
            Guid? personId = null;
            if (parts.Length >= 4 && parts[3].Length > 0)
            {
                if (!Guid.TryParse(parts[3], out var parsedPersonId))
                {
                    AddLineError("Input.CandidateLines", "Candidate line {0}: person ID must be a GUID or empty.", number);
                    continue;
                }
                personId = parsedPersonId;
            }
            if (!candidatesBySort.TryGetValue(partySort, out var candidates))
                candidatesBySort[partySort] = candidates = [];
            if (candidates.Any(candidate => candidate.Position == position))
            {
                AddLineError("Input.CandidateLines", "Candidate line {0}: position is duplicated within the party list.", number);
                continue;
            }
            if (personId.HasValue && candidates.Any(candidate => candidate.PersonId == personId))
            {
                AddLineError("Input.CandidateLines", "Candidate line {0}: person ID is duplicated within the party list.", number);
                continue;
            }
            candidates.Add(new HistoricalCandidateInput(personId, parts[2], parts.Length == 5 ? EmptyToNull(parts[4]) : null, position));
        }

        foreach (var sort in candidatesBySort.Keys.Where(sort => parties.All(party => party.Value.SortOrder != sort)))
            ModelState.AddModelError("Input.CandidateLines", localizer["Candidate party sort order {0} has no matching party list.", sort]);
        if (parties.Count == 0)
            ModelState.AddModelError("Input.PartyListLines", localizer["Enter at least one party-list line."]);

        return parties.Select(party => party.Value with
        {
            Candidates = candidatesBySort.TryGetValue(party.Value.SortOrder, out var candidates) ? candidates : []
        }).ToList();
    }

    private IReadOnlyList<HistoricalReferendumOptionInput> ParseReferendumOptions(string input)
    {
        var options = new List<HistoricalReferendumOptionInput>();
        foreach (var (line, number) in Lines(input))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length is not 4 and not 5)
            {
                AddLineError("Input.ReferendumLines", "Referendum line {0}: expected 4 or 5 fields.", number);
                continue;
            }
            if (!TryInt(parts[0], "Input.ReferendumLines", "Referendum line {0}: sort order must be a whole number.", number, out var sort) |
                !TryInt(parts[3], "Input.ReferendumLines", "Referendum line {0}: votes must be a non-negative whole number.", number, out var votes, nonNegative: true)) continue;
            if (parts[1].Length == 0 || parts[2].Length == 0)
            {
                AddLineError("Input.ReferendumLines", "Referendum line {0}: code and label are required.", number);
                continue;
            }
            if (options.Any(option => option.SortOrder == sort))
            {
                AddLineError("Input.ReferendumLines", "Referendum line {0}: sort order is duplicated.", number);
                continue;
            }
            if (options.Any(option => string.Equals(option.Code, parts[1], StringComparison.OrdinalIgnoreCase)))
            {
                AddLineError("Input.ReferendumLines", "Referendum line {0}: code is duplicated.", number);
                continue;
            }
            options.Add(new HistoricalReferendumOptionInput(parts[1], parts[2], parts.Length == 5 ? EmptyToNull(parts[4]) : null, sort, votes));
        }
        if (options.Count < 2)
            ModelState.AddModelError("Input.ReferendumLines", localizer["Enter at least two referendum lines."]);
        return options;
    }

    private bool TryInt(string value, string key, string message, int line, out int result, bool nonNegative = false, bool positive = false)
    {
        if (int.TryParse(value, out result) && (!nonNegative || result >= 0) && (!positive || result > 0)) return true;
        AddLineError(key, message, line);
        return false;
    }

    private void AddLineError(string key, string message, int line) =>
        ModelState.AddModelError(key, localizer[message, line]);

    private static IEnumerable<(string Text, int Number)> Lines(string input) => input
        .Split(['\r', '\n'])
        .Select((text, index) => (text.Trim(), index + 1))
        .Where(line => line.Item1.Length > 0);

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HistoricalImportForm
{
    [Display(Name = "Record slug"), Required, StringLength(120)] public string Slug { get; set; } = string.Empty;
    [Display(Name = "Title"), Required, StringLength(240)] public string Title { get; set; } = string.Empty;
    [Display(Name = "Description"), Required, StringLength(4000)] public string Description { get; set; } = string.Empty;
    [Display(Name = "Type"), Required] public string Type { get; set; } = "PartyList";
    [Display(Name = "Voting starts"), Required] public DateTimeOffset VotingStartsAt { get; set; } = DateTimeOffset.UtcNow.AddYears(-1);
    [Display(Name = "Voting ends"), Required] public DateTimeOffset VotingEndsAt { get; set; } = DateTimeOffset.UtcNow.AddYears(-1).AddDays(1);
    [Display(Name = "Territory code"), StringLength(80)] public string? TerritoryCode { get; set; }
    [Display(Name = "Record source"), Required, StringLength(1000)] public string SourceReference { get; set; } = string.Empty;
    [Display(Name = "Eligible voters (optional)"), Range(0, int.MaxValue)] public int? EligibleVoterCount { get; set; }
    [Display(Name = "Participating voters"), Range(0, int.MaxValue)] public int ParticipatingVoterCount { get; set; }
    [Display(Name = "Invalid ballots"), Range(0, int.MaxValue)] public int InvalidBallotCount { get; set; }
    [Display(Name = "Party-list lines")] public string PartyListLines { get; set; } = string.Empty;
    [Display(Name = "Candidate lines (optional)")] public string CandidateLines { get; set; } = string.Empty;
    [Display(Name = "Referendum lines")] public string ReferendumLines { get; set; } = string.Empty;
}