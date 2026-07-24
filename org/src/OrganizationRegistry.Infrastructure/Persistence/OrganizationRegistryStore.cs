using Microsoft.EntityFrameworkCore;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Domain.Entities;
using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Infrastructure.Persistence;

public sealed class OrganizationRegistryStore(OrganizationRegistryDbContext db) : IOrganizationRegistryStore
{
    public async Task<IReadOnlyList<Organization>> ListPublicOrganizationsAsync(
        string? search,
        string? classificationCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = PublicOrganizations();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.LegalName, pattern) ||
                (item.TradingName != null && EF.Functions.ILike(item.TradingName, pattern)) ||
                EF.Functions.ILike(item.RegistrationNumber, pattern));
        }
        if (!string.IsNullOrWhiteSpace(classificationCode))
            query = query.Where(item => item.Classifications.Any(value => value.Definition.Code == classificationCode));
        return await query.OrderBy(item => item.LegalName).Skip(skip).Take(take).ToListAsync(ct);
    }

    public Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct) =>
        OrganizationsWithPublicDetails().FirstOrDefaultAsync(item => item.Id == id, ct);

    public Task<Organization?> GetPublicOrganizationAsync(string identifier, CancellationToken ct)
    {
        var hasId = Guid.TryParse(identifier, out var id);
        return PublicOrganizations().FirstOrDefaultAsync(item =>
            (hasId && item.Id == id) || item.Slug == identifier || item.RegistrationNumber == identifier, ct);
    }

    public async Task<IReadOnlyList<Organization>> ListOrganizationsForPersonAsync(Guid personId, CancellationToken ct) =>
        await OrganizationsWithPublicDetails()
            .Include(item => item.AccessGrants)
            .Where(item => item.AccessGrants.Any(grant =>
                grant.PersonId == personId && grant.RevokedAt == null &&
                grant.ValidFrom <= DateTime.UtcNow && (grant.ValidUntil == null || grant.ValidUntil > DateTime.UtcNow)))
            .OrderBy(item => item.LegalName)
            .ToListAsync(ct);

    public Task<bool> HasActiveAccessAsync(
        Guid organizationId,
        Guid personId,
        IReadOnlySet<string> roleCodes,
        DateTime timestamp,
        CancellationToken ct) => db.OrganizationAccessGrants.AnyAsync(grant =>
            grant.OrganizationId == organizationId && grant.PersonId == personId && roleCodes.Contains(grant.RoleCode) &&
            grant.RevokedAt == null && grant.ValidFrom <= timestamp && (grant.ValidUntil == null || grant.ValidUntil > timestamp), ct);

    public Task<RegistrationApplication?> GetApplicationAsync(Guid id, CancellationToken ct) =>
        db.RegistrationApplications.Include(item => item.Transitions).FirstOrDefaultAsync(item => item.Id == id, ct);

    public async Task<IReadOnlyList<RegistrationApplication>> ListApplicationsForPersonAsync(Guid personId, CancellationToken ct) =>
        await db.RegistrationApplications.Where(item => item.ApplicantPersonId == personId).OrderByDescending(item => item.UpdatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<RegistrationApplication>> ListApplicationsByStatusAsync(
        RegistrationApplicationStatus? status,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = db.RegistrationApplications.AsQueryable();
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        return await query.OrderBy(item => item.SubmittedAt ?? item.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ClassificationDefinition>> GetClassificationsAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var normalized = codes.Distinct().ToArray();
        return await db.ClassificationDefinitions.Where(item => item.IsActive && normalized.Contains(item.Code)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ClassificationDefinition>> ListClassificationDefinitionsAsync(CancellationToken ct) =>
        await db.ClassificationDefinitions.Where(item => item.IsActive).OrderBy(item => item.SortOrder).ToListAsync(ct);

    public async Task<IReadOnlyList<OrganizationCorrectionRequest>> ListCorrectionsAsync(Guid organizationId, CancellationToken ct) =>
        await db.OrganizationCorrectionRequests.Where(item => item.OrganizationId == organizationId).OrderByDescending(item => item.SubmittedAt).ToListAsync(ct);

    public Task AddOrganizationAsync(Organization organization, CancellationToken ct) => db.Organizations.AddAsync(organization, ct).AsTask();
    public Task AddApplicationAsync(RegistrationApplication application, CancellationToken ct) => db.RegistrationApplications.AddAsync(application, ct).AsTask();
    public Task AddAccessGrantAsync(OrganizationAccessGrant grant, CancellationToken ct) => db.OrganizationAccessGrants.AddAsync(grant, ct).AsTask();
    public Task AddCorrectionAsync(OrganizationCorrectionRequest correction, CancellationToken ct) => db.OrganizationCorrectionRequests.AddAsync(correction, ct).AsTask();
    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct) => db.Organizations.AnyAsync(item => item.RegistrationNumber == registrationNumber, ct);
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) => db.Organizations.AnyAsync(item => item.Slug == slug, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    private IQueryable<Organization> PublicOrganizations() =>
        OrganizationsWithPublicDetails().Where(item => item.Status != OrganizationStatus.Dissolved);

    private IQueryable<Organization> OrganizationsWithPublicDetails() =>
        db.Organizations
            .Include(item => item.Classifications).ThenInclude(item => item.Definition)
            .Include(item => item.Assets.Where(asset => asset.Visibility == AssetVisibility.Public));
}