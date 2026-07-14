using System.ComponentModel.DataAnnotations;
using Ego.Application.Models;
using Ego.Domain.Enums;

namespace Ego.Api.Models;

public sealed class PatchPersonRequestDto : IValidatableObject
{
    public string? PreferredUsername { get; init; }
    public string? DisplayName { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    public string? Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PreferredUsername is null && DisplayName is null && Email is null && Status is null)
        {
            yield return new ValidationResult("At least one field must be supplied.");
        }

        if (Status is not null && !PersonStatusMapper.TryParse(Status, out _))
        {
            yield return new ValidationResult("Status must be 'active' or 'disabled'.", [nameof(Status)]);
        }
    }
}

public static class PatchPersonRequestDtoMappings
{
    public static PatchPersonCommand ToCommand(this PatchPersonRequestDto request) => new(
        request.PreferredUsername,
        request.DisplayName,
        request.Email,
        PersonStatusMapper.TryParse(request.Status, out var status) ? status : null);
}
