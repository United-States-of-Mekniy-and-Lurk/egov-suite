namespace Ego.Application.Models;

public sealed record IdentityClaims(
    string Subject,
    string PreferredUsername,
    string DisplayName,
    string Email);
