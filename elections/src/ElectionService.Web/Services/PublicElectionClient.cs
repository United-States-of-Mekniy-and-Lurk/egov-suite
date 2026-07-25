using System.Net;
using System.Net.Http.Json;
using ElectionService.Web.Models;

namespace ElectionService.Web.Services;

public sealed class PublicElectionClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ElectionView>> ListAsync(CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<ElectionView>>("/public/elections", ct) ?? [];

    public async Task<ElectionView?> GetAsync(string identifier, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync($"/public/elections/{Uri.EscapeDataString(identifier)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw await ElectionApiException.FromResponseAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ElectionView>(cancellationToken: ct);
    }

    public async Task<OfficialElectionRecordView> RecordAsync(string identifier, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(
            $"/public/elections/{Uri.EscapeDataString(identifier)}/record", ct);
        if (!response.IsSuccessStatusCode) throw await ElectionApiException.FromResponseAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<OfficialElectionRecordView>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<ResultView>> ResultsAsync(Guid electionId, CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<ResultView>>($"/public/elections/{electionId}/results", ct) ?? [];
}