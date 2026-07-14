using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Interfaces;

public interface IApplicationRepository
{
    Task<CitizenshipApplication?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<CitizenshipApplication>> ListByPersonIdAsync(Guid personId, CancellationToken ct);
    Task<IEnumerable<CitizenshipApplication>> ListAsync(ApplicationStatus? statusFilter, int skip, int take, CancellationToken ct);
    Task<CitizenshipApplication> AddAsync(CitizenshipApplication application, CancellationToken ct);
    Task<CitizenshipApplication> UpdateAsync(CitizenshipApplication application, CancellationToken ct);
    Task<int> CountByStatusAsync(ApplicationStatus status, CancellationToken ct);
    Task<IEnumerable<ApplicationTransition>> GetTransitionsAsync(Guid applicationId, CancellationToken ct);
    Task AddTransitionAsync(ApplicationTransition transition, CancellationToken ct);
}
