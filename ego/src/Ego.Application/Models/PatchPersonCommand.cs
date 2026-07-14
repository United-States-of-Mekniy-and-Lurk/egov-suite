using Ego.Domain.Enums;

namespace Ego.Application.Models;

public sealed record PatchPersonCommand(
    string? PreferredUsername,
    string? DisplayName,
    string? Email,
    PersonStatus? Status);
