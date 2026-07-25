using System.Net.Http.Json;
using ElectionService.Application.Abstractions;
using ElectionService.Application.Models;
using Microsoft.AspNetCore.Http;

namespace ElectionService.Infrastructure.Http;

public abstract class BearerForwardingClient(HttpClient httpClient, IHttpContextAccessor accessor)
{
    protected async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        using var response = await httpClient.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }
}

public sealed class OrganizationRegistryClient(HttpClient client, IHttpContextAccessor accessor)
    : BearerForwardingClient(client, accessor), IOrganizationRegistryClient
{
    public async Task<OrganizationSnapshot?> GetAsync(Guid organizationId, CancellationToken ct)
    {
        var organization = await GetAsync<OrganizationPayload>($"/public/organizations/{organizationId}", ct);
        return organization is null ? null : new OrganizationSnapshot(organization.Id, organization.RegistrationNumber,
            organization.LegalName, organization.Status, organization.Classifications.Select(item => item.Code).ToList());
    }

    private sealed record OrganizationPayload(Guid Id, string RegistrationNumber, string LegalName, string Status, List<ClassificationPayload> Classifications);
    private sealed record ClassificationPayload(string Code);
}

public sealed class CitizenRegistryClient(HttpClient client, IHttpContextAccessor accessor)
    : BearerForwardingClient(client, accessor), ICitizenRegistryClient
{
    public async Task<CitizenSnapshot?> GetAsync(Guid personId, CancellationToken ct)
    {
        var citizen = await GetAsync<CitizenPayload>($"/citizens/{personId}", ct);
        return citizen is null ? null : new CitizenSnapshot(citizen.PersonId, citizen.Status);
    }
    private sealed record CitizenPayload(Guid PersonId, string Status);
}

public sealed class PersonRegistryClient(HttpClient client, IHttpContextAccessor accessor)
    : BearerForwardingClient(client, accessor), IPersonRegistryClient
{
    public async Task<PersonSnapshot?> GetAsync(Guid personId, CancellationToken ct)
    {
        var person = await GetAsync<PersonPayload>($"/persons/{personId}", ct);
        return person is null ? null : new PersonSnapshot(person.Id, person.DisplayName);
    }
    private sealed record PersonPayload(Guid Id, string DisplayName);
}