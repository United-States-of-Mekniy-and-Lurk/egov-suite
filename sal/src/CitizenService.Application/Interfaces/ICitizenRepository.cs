using CitizenService.Domain.Entities;

namespace CitizenService.Application.Interfaces;

public interface ICitizenRepository
{
    Task<Citizen?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Citizen?> GetByPersonIdAsync(Guid personId, CancellationToken ct);
    Task<IEnumerable<Citizen>> ListAsync(int skip, int take, CancellationToken ct);
    Task<Citizen> AddAsync(Citizen citizen, CancellationToken ct);
    Task<Citizen> UpdateAsync(Citizen citizen, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}
