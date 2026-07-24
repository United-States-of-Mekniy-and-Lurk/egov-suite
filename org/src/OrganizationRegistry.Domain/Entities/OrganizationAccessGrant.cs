namespace OrganizationRegistry.Domain.Entities;

public sealed class OrganizationAccessGrant
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public Guid PersonId { get; set; }
    public string RoleCode { get; set; } = OrganizationAccessRoles.Viewer;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid GrantedByPersonId { get; set; }

    public bool IsActiveAt(DateTime timestamp) =>
        RevokedAt == null && ValidFrom <= timestamp && (ValidUntil == null || ValidUntil > timestamp);
}

public static class OrganizationAccessRoles
{
    public const string Owner = "owner";
    public const string Administrator = "administrator";
    public const string Representative = "representative";
    public const string Viewer = "viewer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Owner,
        Administrator,
        Representative,
        Viewer
    };
}