using System.ComponentModel.DataAnnotations;
using Ego.Application.Models;
using Ego.Domain.Enums;

namespace Ego.Api.Models;

public sealed class CreatePersonRequestDto : IValidatableObject
{
    [Required]
    public string IdentitySubject { get; init; } = string.Empty;

    [Required]
    public string PreferredUsername { get; init; } = string.Empty;

    [Required]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    public string? Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !PersonStatusMapper.TryParse(Status, out _))
        {
            yield return new ValidationResult("Status must be 'active' or 'disabled'.", [nameof(Status)]);
        }
    }
}

public static class CreatePersonRequestDtoMappings
{
    public static CreatePersonCommand ToCommand(this CreatePersonRequestDto request) => new(
        request.IdentitySubject,
        request.PreferredUsername,
        request.DisplayName,
        request.Email,
        PersonStatusMapper.TryParse(request.Status, out var status) ? status : null);
}

internal static class PersonStatusMapper
{
    public static bool TryParse(string? value, out PersonStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.ToLowerInvariant())
        {
            case "active":
                status = PersonStatus.Active;
                return true;
            case "disabled":
                status = PersonStatus.Disabled;
                return true;
            default:
                return false;
        }
    }
}
