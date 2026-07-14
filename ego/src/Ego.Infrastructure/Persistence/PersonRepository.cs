using Ego.Application.Abstractions;
using Ego.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ego.Infrastructure.Persistence;

public class PersonRepository(EgoDbContext dbContext) : IPersonRepository
{
    public async Task<Person?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Persons.FindAsync([id], ct);
    }

    public Task<Person?> FindByIdentitySubjectAsync(string identitySubject, CancellationToken ct = default)
    {
        return dbContext.Persons.FirstOrDefaultAsync(person => person.IdentitySubject == identitySubject, ct);
    }

    public Task AddAsync(Person person, CancellationToken ct = default)
    {
        return dbContext.Persons.AddAsync(person, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}
