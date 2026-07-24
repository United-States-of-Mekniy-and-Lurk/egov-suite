using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Domain.Entities;

public sealed class OrganizationAsset
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationAssetKind Kind { get; set; }
    public AssetVisibility Visibility { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByPersonId { get; set; }
}