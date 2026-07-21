using CitizenService.Domain.Enums;

namespace CitizenService.Application.Models;

public sealed record FieldCorrectionRequestDto(
    Guid Id,
    Guid CitizenId,
    Guid PersonId,
    RegistryFieldDefinitionDto Definition,
    string? CurrentValue,
    string ProposedValue,
    string RequestReason,
    FieldCorrectionStatus Status,
    Guid RequestedByPersonId,
    DateTime SubmittedAt,
    Guid? ReviewedByPersonId,
    DateTime? ReviewedAt,
    string? ReviewReason);

public sealed record SubmitFieldCorrectionInput(string? ProposedValue, string RequestReason);

public sealed record ReviewFieldCorrectionInput(string Reason);