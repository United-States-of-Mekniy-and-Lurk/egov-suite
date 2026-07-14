using Ego.Domain.Entities;

namespace Ego.Api.Models;

public sealed record PersonDto(
    Guid Id,
    string IdentitySubject,
    string PreferredUsername,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class PersonMappings
{
    public static PersonDto ToDto(this Person person) => new(
        person.Id,
        person.IdentitySubject,
        person.PreferredUsername,
        person.DisplayName,
        person.Email,
        person.Status.ToString().ToLowerInvariant(),
        person.CreatedAt,
        person.UpdatedAt);
}
