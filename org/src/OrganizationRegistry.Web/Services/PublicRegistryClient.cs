using System.Net;
using System.Net.Http.Json;
using OrganizationRegistry.Web.Models;

namespace OrganizationRegistry.Web.Services;

public sealed class PublicRegistryClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PublicOrganization>> ListOrganizationsAsync(
        string? search,
        string? classification,
        CancellationToken ct)
    {
        var query = new List<string> { "take=50" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(classification))
            query.Add($"classification={Uri.EscapeDataString(classification.Trim())}");

        return await httpClient.GetFromJsonAsync<List<PublicOrganization>>(
            $"/public/organizations?{string.Join('&', query)}", ct) ?? [];
    }

    public async Task<IReadOnlyList<Classification>> ListClassificationsAsync(CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<Classification>>("/public/classifications", ct) ?? [];

    public async Task<PublicOrganization?> GetOrganizationAsync(string identifier, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(
            $"/public/organizations/{Uri.EscapeDataString(identifier)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PublicOrganization>(cancellationToken: ct);
    }
}