using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Domain.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string LegalFormCode { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RegisteredAddress { get; set; } = string.Empty;
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
    public DateTime RegisteredAt { get; set; }
    public DateOnly? EstablishedOn { get; set; }
    public string? ImportSourceReference { get; set; }
    public string? ImportNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public List<OrganizationClassification> Classifications { get; set; } = [];
    public List<OrganizationAccessGrant> AccessGrants { get; set; } = [];
    public List<OrganizationAsset> Assets { get; set; } = [];
}