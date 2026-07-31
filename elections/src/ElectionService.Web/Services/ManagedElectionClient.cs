using System.Net.Http.Json;
using ElectionService.Web.Models;

namespace ElectionService.Web.Services;

public sealed class ManagedElectionClient(
    HttpClient httpClient,
    ILogger<ManagedElectionClient> logger)
{
    public Task<IReadOnlyList<ElectionView>> ListAsync(CancellationToken ct) =>
        GetAsync<IReadOnlyList<ElectionView>>("/admin/elections", ct);

    public Task<ElectionView> GetAsync(Guid electionId, CancellationToken ct) =>
        GetAsync<ElectionView>($"/admin/elections/{electionId}", ct);

    public Task<BallotReceipt> VoteAsync(Guid electionId, VoteInput input, CancellationToken ct) =>
        SendAsync<BallotReceipt>(HttpMethod.Post, $"/elections/{electionId}/vote", input, ct);

    public Task<ElectionView> CreateAsync(ElectionInput input, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Post, "/admin/elections", ToApiInput(input), ct);

    public Task<ElectionView> ImportHistoricalAsync(HistoricalElectionInput input, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Post, "/admin/historical-elections", input, ct);

    public Task<ElectionView> UpdateAsync(Guid electionId, ElectionInput input, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Put, $"/admin/elections/{electionId}", ToApiInput(input), ct);

    public Task<PartyListView> AddPartyListAsync(Guid electionId, PartyListInput input, CancellationToken ct) =>
        SendAsync<PartyListView>(HttpMethod.Post, $"/admin/elections/{electionId}/party-lists", input, ct);

    public Task<PartyListView> UpdatePartyListAsync(Guid electionId, Guid partyListId, PartyListInput input, CancellationToken ct) =>
        SendAsync<PartyListView>(HttpMethod.Put, $"/admin/elections/{electionId}/party-lists/{partyListId}", input, ct);

    public Task DeletePartyListAsync(Guid electionId, Guid partyListId, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Delete, $"/admin/elections/{electionId}/party-lists/{partyListId}", null, ct);

    public Task<CandidateView> AddCandidateAsync(Guid electionId, Guid partyListId, CandidateInput input, CancellationToken ct) =>
        SendAsync<CandidateView>(HttpMethod.Post, $"/admin/elections/{electionId}/party-lists/{partyListId}/candidates", input, ct);

    public Task<CandidateView> UpdateCandidateAsync(Guid electionId, Guid partyListId, Guid candidateId, CandidateInput input, CancellationToken ct) =>
        SendAsync<CandidateView>(HttpMethod.Put, $"/admin/elections/{electionId}/party-lists/{partyListId}/candidates/{candidateId}", input, ct);

    public Task DeleteCandidateAsync(Guid electionId, Guid partyListId, Guid candidateId, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Delete, $"/admin/elections/{electionId}/party-lists/{partyListId}/candidates/{candidateId}", null, ct);

    public Task WithdrawCandidateAsync(Guid electionId, Guid partyListId, Guid candidateId, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Post, $"/admin/elections/{electionId}/party-lists/{partyListId}/candidates/{candidateId}/withdraw", new { }, ct);

    public Task<ElectionView> UpdateScheduleAsync(Guid electionId, DateTime votingStartsAt, DateTime votingEndsAt, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Put, $"/admin/elections/{electionId}/schedule", new { votingStartsAt, votingEndsAt }, ct);

    public Task<ReferendumOptionView> AddOptionAsync(Guid electionId, ReferendumOptionInput input, CancellationToken ct) =>
        SendAsync<ReferendumOptionView>(HttpMethod.Post, $"/admin/elections/{electionId}/referendum-options", input, ct);

    public Task<ReferendumOptionView> UpdateOptionAsync(Guid electionId, Guid optionId, ReferendumOptionInput input, CancellationToken ct) =>
        SendAsync<ReferendumOptionView>(HttpMethod.Put, $"/admin/elections/{electionId}/referendum-options/{optionId}", input, ct);

    public Task DeleteOptionAsync(Guid electionId, Guid optionId, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Delete, $"/admin/elections/{electionId}/referendum-options/{optionId}", null, ct);

    public Task<IReadOnlyList<VoterRollEntryView>> ListVoterRollAsync(Guid electionId, CancellationToken ct) =>
        GetAsync<IReadOnlyList<VoterRollEntryView>>($"/admin/elections/{electionId}/voter-roll", ct);

    public Task AddVoterAsync(Guid electionId, VoterRollInput input, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Post, $"/admin/elections/{electionId}/voter-roll", input, ct);

    public Task<int> BulkAddVotersAsync(Guid electionId, BulkVoterRollInput input, CancellationToken ct) =>
        SendAsync<int>(HttpMethod.Post, $"/admin/elections/{electionId}/voter-roll/bulk", input, ct);

    public Task RemoveVoterAsync(Guid electionId, Guid personId, CancellationToken ct) =>
        SendWithoutResponseAsync(HttpMethod.Delete, $"/admin/elections/{electionId}/voter-roll/{personId}", null, ct);

    public Task<IReadOnlyList<InvitationAdminView>> ListInvitationsAsync(Guid electionId, CancellationToken ct) =>
        GetAsync<IReadOnlyList<InvitationAdminView>>($"/admin/elections/{electionId}/invitations", ct);

    public Task<InvitationCreated> CreateInvitationAsync(Guid electionId, InvitationInput input, CancellationToken ct) =>
        SendAsync<InvitationCreated>(HttpMethod.Post, $"/admin/elections/{electionId}/invitations", input, ct);

    public Task<IReadOnlyList<InvitationCreated>> BulkCreateInvitationsAsync(Guid electionId, BulkInvitationInput input, CancellationToken ct) =>
        SendAsync<IReadOnlyList<InvitationCreated>>(HttpMethod.Post, $"/admin/elections/{electionId}/invitations/bulk", input, ct);

    public Task<InvitationAdminView> RevokeInvitationAsync(Guid electionId, Guid invitationId, CancellationToken ct) =>
        SendAsync<InvitationAdminView>(HttpMethod.Delete, $"/admin/elections/{electionId}/invitations/{invitationId}", new { }, ct);

    public Task<ElectionView> TransitionAsync(Guid electionId, TransitionInput input, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Post, $"/admin/elections/{electionId}/transitions", input, ct);

    public Task<ElectionView> ForceCertifyAsync(Guid electionId, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Post, $"/admin/elections/{electionId}/force-certify", new { }, ct);

    public Task<ElectionView> SetWinnersAsync(Guid electionId, WinnerSelectionInput input, CancellationToken ct) =>
        SendAsync<ElectionView>(HttpMethod.Put, $"/admin/elections/{electionId}/winners", input, ct);

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Election API request failed method={Method} path={Path} status={StatusCode}",
                HttpMethod.Get,
                path,
                (int)response.StatusCode);
            throw await ElectionApiException.FromResponseAsync(response, ct);
        }
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object input, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(input) };
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Election API request failed method={Method} path={Path} status={StatusCode}",
                method,
                path,
                (int)response.StatusCode);
            throw await ElectionApiException.FromResponseAsync(response, ct);
        }
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task SendWithoutResponseAsync(HttpMethod method, string path, object? input, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (input is not null) request.Content = JsonContent.Create(input);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Election API request failed method={Method} path={Path} status={StatusCode}",
                method,
                path,
                (int)response.StatusCode);
            throw await ElectionApiException.FromResponseAsync(response, ct);
        }
    }

    private static object ToApiInput(ElectionInput input) => new
    {
        input.Slug,
        input.Title,
        input.Description,
        input.Type,
        input.EligibilityMode,
        VotingStartsAt = input.VotingStartsAtUtc,
        VotingEndsAt = input.VotingEndsAtUtc,
        input.TerritoryCode,
        input.EligibleVoterCount,
        input.SeatCount
    };
}