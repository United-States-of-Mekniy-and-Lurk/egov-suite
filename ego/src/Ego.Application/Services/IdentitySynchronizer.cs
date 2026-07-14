using Ego.Application.Abstractions;
using Ego.Application.Models;
using Ego.Domain.Entities;

namespace Ego.Application.Services;

public class IdentitySynchronizer(IPersonRepository personRepository) : IIdentitySynchronizer
{
    public async Task<Person> SynchronizeAsync(IdentityClaims claims, CancellationToken ct = default)
    {
        var person = await personRepository.FindByIdentitySubjectAsync(claims.Subject, ct);
        if (person is null)
        {
            person = Person.Create(
                claims.Subject,
                claims.PreferredUsername,
                claims.DisplayName,
                claims.Email);

            await personRepository.AddAsync(person, ct);
        }
        else
        {
            person.UpdateIdentityClaims(claims.PreferredUsername, claims.DisplayName, claims.Email);
        }

        await personRepository.SaveChangesAsync(ct);

        return person;
    }
}
