namespace OrganizationRegistry.Web.Models;

public sealed record Classification(string Scheme, string Code, string LabelEn, string LabelCs)
{
    public string Label(string culture) => culture == "cs" ? LabelCs : LabelEn;
}

public sealed record PublicOrganization(
    Guid Id,
    string RegistrationNumber,
    string Slug,
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    string Status,
    DateTime RegisteredAt,
    DateOnly? EstablishedOn,
    IReadOnlyList<Classification> Classifications,
    string? LogoUrl);

public sealed record ManagedOrganization(PublicOrganization Organization, IReadOnlyList<string> Roles);

public sealed record RegistrationApplication(
    Guid Id,
    Guid ApplicantPersonId,
    string Status,
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

public sealed record RegistrationApplicationInput(
    string LegalName,
    string? TradingName,
    string LegalFormCode,
    string Purpose,
    string RegisteredAddress,
    string[] ClassificationCodes);

public sealed record HistoricalOrganizationInput(
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

public sealed record CorrectionRequest(
    Guid Id,
    Guid OrganizationId,
    string FieldKey,
    string? CurrentValue,
    string? ProposedValue,
    string Reason,
    string Status,
    DateTime SubmittedAt,
    string? ReviewReason);