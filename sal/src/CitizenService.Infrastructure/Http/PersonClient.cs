using Microsoft.AspNetCore.Http;

namespace CitizenService.Infrastructure.Http;

public class PersonClient : IPersonClient
{
    private readonly IPersonRegistryApi _api;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PersonClient(IPersonRegistryApi api, IHttpContextAccessor httpContextAccessor)
    {
        _api = api;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PersonDto?> GetPersonAsync(Guid personId, CancellationToken ct)
    {
        var response = await _api.GetPersonByIdAsync(personId, GetAuthorizationHeader(), ct);
        EnsureAuthorized(response.StatusCode);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        EnsureSuccessful(response.IsSuccessStatusCode, response.StatusCode);
        if (response.Content == null)
            throw new InvalidOperationException("The Person Registry returned an empty person response.");

        var p = response.Content;
        return new PersonDto
        {
            Id = p.Id,
            IdentitySubject = p.IdentitySubject,
            PreferredUsername = p.PreferredUsername,
            DisplayName = p.DisplayName,
            Email = p.Email,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    public async Task<bool> PersonExistsAsync(Guid personId, CancellationToken ct)
    {
        var response = await _api.GetPersonByIdAsync(personId, GetAuthorizationHeader(), ct);
        EnsureAuthorized(response.StatusCode);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;
        EnsureSuccessful(response.IsSuccessStatusCode, response.StatusCode);
        return true;
    }

    private string GetAuthorizationHeader()
        => _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString()
            is { Length: > 0 } authorization
                ? authorization
                : throw new InvalidOperationException("The current request does not contain an authorization header.");

    private static void EnsureAuthorized(System.Net.HttpStatusCode? statusCode)
    {
        if (statusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            throw new InvalidOperationException("The Person Registry rejected the service request.");
    }

    private static void EnsureSuccessful(bool isSuccessStatusCode, System.Net.HttpStatusCode? statusCode)
    {
        if (!isSuccessStatusCode)
            throw new InvalidOperationException(
                $"The Person Registry request failed with status {(int?)statusCode ?? 0}.");
    }
}
