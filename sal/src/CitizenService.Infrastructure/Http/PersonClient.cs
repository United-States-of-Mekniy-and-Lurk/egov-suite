using CitizenService.Application.Interfaces;

namespace CitizenService.Infrastructure.Http;

public class PersonClient : IPersonClient
{
    private readonly IPersonRegistryApi _api;

    public PersonClient(IPersonRegistryApi api)
    {
        _api = api;
    }

    public async Task<PersonDto?> GetPersonAsync(Guid personId, CancellationToken ct)
    {
        var response = await _api.GetPersonByIdAsync(personId, ct);
        if (!response.IsSuccessStatusCode || response.Content == null)
            return null;

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
        var response = await _api.GetPersonByIdAsync(personId, ct);
        return response.IsSuccessStatusCode;
    }
}
