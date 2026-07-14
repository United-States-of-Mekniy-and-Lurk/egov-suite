using Ego.Domain.Enums;

namespace Ego.Domain.Entities;

public class Person
{
    public Guid Id { get; private set; }
    public string IdentitySubject { get; private set; } = string.Empty;
    public string PreferredUsername { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public PersonStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Person()
    {
    }

    public static Person Create(
        string identitySubject,
        string preferredUsername,
        string displayName,
        string email,
        PersonStatus status = PersonStatus.Active)
    {
        var now = DateTimeOffset.UtcNow;

        return new Person
        {
            Id = Guid.NewGuid(),
            IdentitySubject = identitySubject,
            PreferredUsername = preferredUsername,
            DisplayName = displayName,
            Email = email,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateIdentityClaims(string preferredUsername, string displayName, string email)
    {
        PreferredUsername = preferredUsername;
        DisplayName = displayName;
        Email = email;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(PersonStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
