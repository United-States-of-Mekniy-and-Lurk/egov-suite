using Ego.Domain.Entities;

namespace Ego.Application.Abstractions;

public interface IPersonRepository
{
    Task<Person?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Person?> FindByIdentitySubjectAsync(string identitySubject, CancellationToken ct = default);
    Task AddAsync(Person person, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
