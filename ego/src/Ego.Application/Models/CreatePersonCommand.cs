using Ego.Domain.Enums;

namespace Ego.Application.Models;

public sealed record CreatePersonCommand(
    string IdentitySubject,
    string PreferredUsername,
    string DisplayName,
    string Email,
    PersonStatus? Status);
