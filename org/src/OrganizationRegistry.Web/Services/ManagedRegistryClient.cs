using System.Net.Http.Json;
using OrganizationRegistry.Web.Models;

namespace OrganizationRegistry.Web.Services;

public sealed class ManagedRegistryClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ManagedOrganization>> ListOrganizationsAsync(CancellationToken ct) =>
        await GetAsync<List<ManagedOrganization>>("/organizations/mine", ct) ?? [];

    public async Task<IReadOnlyList<RegistrationApplication>> ListApplicationsAsync(CancellationToken ct) =>
        await GetAsync<List<RegistrationApplication>>("/registration-applications/mine", ct) ?? [];

    public Task<RegistrationApplication?> GetApplicationAsync(Guid id, CancellationToken ct) =>
        GetAsync<RegistrationApplication>($"/registration-applications/{id}", ct);

    public async Task<IReadOnlyList<RegistrationApplication>> ListQueueAsync(string? status, CancellationToken ct)
    {
        var path = string.IsNullOrWhiteSpace(status)
            ? "/registration-applications/queue"
            : $"/registration-applications/queue?status={Uri.EscapeDataString(status)}";
        return await GetAsync<List<RegistrationApplication>>(path, ct) ?? [];
    }

    public async Task<RegistrationApplication> CreateApplicationAsync(
        RegistrationApplicationInput input,
        CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("/registration-applications", input, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationApplication>(cancellationToken: ct))!;
    }

    public async Task<RegistrationApplication> UpdateApplicationAsync(
        Guid id,
        RegistrationApplicationInput input,
        CancellationToken ct)
    {
        using var response = await httpClient.PutAsJsonAsync($"/registration-applications/{id}", input, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationApplication>(cancellationToken: ct))!;
    }

    public async Task TransitionAsync(Guid id, string targetStatus, string? reason, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/registration-applications/{id}/transitions",
            new { targetStatus, reason },
            ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PublicOrganization> CreateHistoricalOrganizationAsync(
        HistoricalOrganizationInput input,
        CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync("/staff/historical-organizations", input, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicOrganization>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<CorrectionRequest>> ListCorrectionsAsync(Guid organizationId, CancellationToken ct) =>
        await GetAsync<List<CorrectionRequest>>($"/organizations/{organizationId}/correction-requests", ct) ?? [];

    public async Task CreateCorrectionAsync(
        Guid organizationId,
        string fieldKey,
        string? proposedValue,
        string reason,
        CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/organizations/{organizationId}/correction-requests",
            new { fieldKey, proposedValue, reason },
            ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }
}