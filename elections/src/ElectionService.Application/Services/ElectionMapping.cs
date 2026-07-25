using ElectionService.Application.Models;
using ElectionService.Domain.Entities;

namespace ElectionService.Application.Services;

internal static class ElectionMapping
{
    public static ElectionView ToView(this Election election) => new(
        election.Id,
        election.Slug,
        election.Title,
        election.Description,
        election.Type,
        election.Status,
        election.EligibilityMode,
        election.VotingStartsAt,
        election.VotingEndsAt,
        election.TerritoryCode,
        election.PartyLists.OrderBy(item => item.SortOrder).Select(item => new PartyListView(
            item.Id,
            item.PartyOrganizationId,
            item.PartyRegistrationNumber,
            item.PartyName,
            item.ListName,
            item.SortOrder,
            item.Candidates.OrderBy(candidate => candidate.Position).Select(candidate => new CandidateView(
                candidate.Id, candidate.DisplayName, candidate.Description, candidate.Position)).ToList())).ToList(),
        election.ReferendumOptions.OrderBy(item => item.SortOrder).Select(item => new ReferendumOptionView(
            item.Id, item.Code, item.Label, item.Description, item.SortOrder)).ToList(),
        election.IsHistorical,
        election.HistoricalSourceReference);
}