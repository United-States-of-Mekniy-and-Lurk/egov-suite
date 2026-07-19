using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Services;

public class CitizenAppService
{
    private readonly ICitizenRepository _citizenRepository;
    private readonly ICurrentActor _currentActor;
    private readonly IPersonClient _personClient;
    private readonly ICitizenNumberGenerator _citizenNumberGenerator;

    public CitizenAppService(
        ICitizenRepository citizenRepository,
        ICurrentActor currentActor,
        IPersonClient personClient,
        ICitizenNumberGenerator citizenNumberGenerator)
    {
        _citizenRepository = citizenRepository;
        _currentActor = currentActor;
        _personClient = personClient;
        _citizenNumberGenerator = citizenNumberGenerator;
    }

    public async Task<Citizen> CreateCitizenAsync(Guid personId, DateTime? grantedAt, string? importSource, string? citizenNumber, CancellationToken ct)
    {
        var personExists = await _personClient.PersonExistsAsync(personId, ct);
        if (!personExists)
            throw new InvalidOperationException($"Person {personId} not found in the Person Registry.");

        return await CreateVerifiedCitizenAsync(personId, grantedAt, importSource, citizenNumber, ct);
    }

    internal async Task<Citizen> CreateVerifiedCitizenAsync(
        Guid personId,
        DateTime? grantedAt,
        string? importSource,
        string? citizenNumber,
        CancellationToken ct)
    {

        if (string.IsNullOrWhiteSpace(citizenNumber))
            citizenNumber = _citizenNumberGenerator.Generate();

        var citizen = new Citizen
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CitizenNumber = citizenNumber,
            Status = CitizenStatus.Active,
            GrantedAt = grantedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ImportSource = importSource,
            CreatedByPersonId = _currentActor.PersonId
        };

        return await _citizenRepository.AddAsync(citizen, ct);
    }

    public async Task<Citizen> ChangeStatusAsync(Guid personId, CitizenStatus newStatus, string? reason, CancellationToken ct)
    {
        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new InvalidOperationException($"Citizen not found for PersonId {personId}.");

        citizen.Status = newStatus;
        citizen.UpdatedAt = DateTime.UtcNow;

        return await _citizenRepository.UpdateAsync(citizen, ct);
    }

    public async Task<IEnumerable<Citizen>> ListAsync(int skip, int take, CancellationToken ct)
        => await _citizenRepository.ListAsync(skip, take, ct);

    public async Task<Citizen?> GetByPersonIdAsync(Guid personId, CancellationToken ct)
        => await _citizenRepository.GetByPersonIdAsync(personId, ct);

    public async Task<int> GetCountAsync(CancellationToken ct)
        => await _citizenRepository.CountAsync(ct);
}
