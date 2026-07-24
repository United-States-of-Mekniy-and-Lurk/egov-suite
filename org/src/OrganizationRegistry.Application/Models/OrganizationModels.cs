using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Application.Models;

public sealed record ClassificationView(string Scheme, string Code, string LabelEn, string LabelCs);

public sealed record PublicOrganizationView(
    Guid Id,
    string RegistrationNumber,
    string Slug,
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    OrganizationStatus Status,
    DateTime RegisteredAt,
    DateOnly? EstablishedOn,
    IReadOnlyList<ClassificationView> Classifications,
    string? LogoUrl);

public sealed record ManagedOrganizationView(
    PublicOrganizationView Organization,
    IReadOnlyList<string> Roles);

public sealed record CreateRegistrationInput(
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    string[] ClassificationCodes);

public sealed record UpdateRegistrationInput(
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    string[] ClassificationCodes);

public sealed record RegistrationApplicationView(
    Guid Id,
    Guid ApplicantPersonId,
    RegistrationApplicationStatus Status,
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    IReadOnlyList<string> ClassificationCodes,
    Guid? OrganizationId,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? DecisionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TransitionRegistrationInput(RegistrationApplicationStatus TargetStatus, string? Reason);

public sealed record CreateHistoricalOrganizationInput(
    string RegistrationNumber,
    DateOnly RegisteredOn,
    DateOnly? EstablishedOn,
    string SourceReference,
    string? ImportNote,
    Guid? OwnerPersonId,
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    string[] ClassificationCodes);

public sealed record CreateCorrectionInput(string FieldKey, string? ProposedValue, string Reason);

public sealed record CorrectionView(
    Guid Id,
    Guid OrganizationId,
    string FieldKey,
    string? CurrentValue,
    string? ProposedValue,
    string Reason,
    CorrectionRequestStatus Status,
    DateTime SubmittedAt,
    string? ReviewReason);