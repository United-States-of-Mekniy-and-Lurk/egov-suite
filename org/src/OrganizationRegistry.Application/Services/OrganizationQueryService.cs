using Egov.Platform.Identity;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;

namespace OrganizationRegistry.Application.Services;

public sealed class OrganizationQueryService(IOrganizationRegistryStore store, ICurrentActor actor)
{
    public async Task<IReadOnlyList<ClassificationView>> ListClassificationsAsync(CancellationToken ct) =>
        (await store.ListClassificationDefinitionsAsync(ct))
            .Select(item => new ClassificationView(item.Scheme, item.Code, item.LabelEn, item.LabelCs))
            .ToList();

    public async Task<IReadOnlyList<PublicOrganizationView>> ListPublicAsync(
        string? search,
        string? classificationCode,
        int skip,
        int take,
        CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(skip, 0);
        var organizations = await store.ListPublicOrganizationsAsync(search, classificationCode, skip, take, ct);
        return organizations.Select(organization => organization.ToPublicView()).ToList();
    }

    public async Task<PublicOrganizationView> GetPublicAsync(string identifier, CancellationToken ct)
    {
        var organization = await store.GetPublicOrganizationAsync(identifier, ct)
            ?? throw new RegistryNotFoundException("Organization not found.");
        return organization.ToPublicView();
    }

    public async Task<IReadOnlyList<ManagedOrganizationView>> ListMineAsync(CancellationToken ct)
    {
        EnsurePerson();
        var now = DateTime.UtcNow;
        var organizations = await store.ListOrganizationsForPersonAsync(actor.PersonId, ct);
        return organizations.Select(organization => new ManagedOrganizationView(
            organization.ToPublicView(),
            organization.AccessGrants
                .Where(grant => grant.PersonId == actor.PersonId && grant.IsActiveAt(now))
                .Select(grant => grant.RoleCode)
                .Distinct()
                .Order()
                .ToList())).ToList();
    }

    public async Task EnsureAccessAsync(Guid organizationId, IReadOnlySet<string> roleCodes, CancellationToken ct)
    {
        EnsurePerson();
        if (IsStaff()) return;
        if (!await store.HasActiveAccessAsync(organizationId, actor.PersonId, roleCodes, DateTime.UtcNow, ct))
            throw new RegistryForbiddenException("You do not have access to this organization.");
    }

    private void EnsurePerson()
    {
        if (actor.PersonId == Guid.Empty)
            throw new RegistryForbiddenException("A person identity is required.");
    }

    private bool IsStaff() => actor.IsInRole("organization-registry:clerk") || actor.IsInRole("organization-registry:admin");
}