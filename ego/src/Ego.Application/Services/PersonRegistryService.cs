using Ego.Application.Abstractions;
using Ego.Application.Exceptions;
using Ego.Application.Models;
using Ego.Domain.Entities;
using Ego.Domain.Enums;

namespace Ego.Application.Services;

public class PersonRegistryService(IPersonRepository personRepository)
{
    public async Task<Person> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await personRepository.FindByIdAsync(id, ct)
            ?? throw new PersonNotFoundException(id);
    }

    public async Task<Person> GetByIdentitySubjectAsync(string sub, CancellationToken ct = default)
    {
        return await personRepository.FindByIdentitySubjectAsync(sub, ct)
            ?? throw new PersonNotFoundException(sub);
    }

    public async Task<Person> CreateAsync(CreatePersonCommand command, CancellationToken ct = default)
    {
        var existingPerson = await personRepository.FindByIdentitySubjectAsync(command.IdentitySubject, ct);
        if (existingPerson is not null)
        {
            throw new PersonAlreadyExistsException(command.IdentitySubject);
        }

        var person = Person.Create(
            command.IdentitySubject,
            command.PreferredUsername,
            command.DisplayName,
            command.Email,
            command.Status ?? PersonStatus.Active);

        await personRepository.AddAsync(person, ct);
        await personRepository.SaveChangesAsync(ct);

        return person;
    }

    public async Task<Person> PatchAsync(Guid id, PatchPersonCommand command, CancellationToken ct = default)
    {
        var person = await personRepository.FindByIdAsync(id, ct)
            ?? throw new PersonNotFoundException(id);

        if (command.PreferredUsername is not null || command.DisplayName is not null || command.Email is not null)
        {
            person.UpdateIdentityClaims(
                command.PreferredUsername ?? person.PreferredUsername,
                command.DisplayName ?? person.DisplayName,
                command.Email ?? person.Email);
        }

        if (command.Status.HasValue)
        {
            person.UpdateStatus(command.Status.Value);
        }

        await personRepository.SaveChangesAsync(ct);

        return person;
    }
}
