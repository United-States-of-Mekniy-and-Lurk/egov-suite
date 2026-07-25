using System.Net.Http.Json;
using ElectionService.Web.Models;

namespace ElectionService.Web.Services;

public sealed class InvitationElectionClient(HttpClient httpClient)
{
    public async Task<InvitationDetail> GetInvitationAsync(Guid electionId, string token, CancellationToken ct) =>
        await GetAsync<InvitationDetail>($"/invitations/{electionId}/{Uri.EscapeDataString(token)}", ct);

    public async Task<ElectionView> GetElectionAsync(Guid electionId, CancellationToken ct) =>
        await GetAsync<ElectionView>($"/public/elections/{electionId}", ct);

    public async Task<BallotReceipt> VoteAsync(Guid electionId, string token, VoteInput input, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/invitations/{electionId}/{Uri.EscapeDataString(token)}/vote", input, ct);
        if (!response.IsSuccessStatusCode) throw await ElectionApiException.FromResponseAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<BallotReceipt>(cancellationToken: ct))!;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode) throw await ElectionApiException.FromResponseAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }
}