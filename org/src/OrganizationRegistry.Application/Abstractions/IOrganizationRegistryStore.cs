using OrganizationRegistry.Domain.Entities;
using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Application.Abstractions;

public interface IOrganizationRegistryStore
{
    Task<IReadOnlyList<Organization>> ListPublicOrganizationsAsync(string? search, string? classificationCode, int skip, int take, CancellationToken ct);
    Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct);
    Task<Organization?> GetPublicOrganizationAsync(string identifier, CancellationToken ct);
    Task<IReadOnlyList<Organization>> ListOrganizationsForPersonAsync(Guid personId, CancellationToken ct);
    Task<bool> HasActiveAccessAsync(Guid organizationId, Guid personId, IReadOnlySet<string> roleCodes, DateTime timestamp, CancellationToken ct);
    Task<RegistrationApplication?> GetApplicationAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<RegistrationApplication>> ListApplicationsForPersonAsync(Guid personId, CancellationToken ct);
    Task<IReadOnlyList<RegistrationApplication>> ListApplicationsByStatusAsync(RegistrationApplicationStatus? status, int skip, int take, CancellationToken ct);
    Task<IReadOnlyList<ClassificationDefinition>> GetClassificationsAsync(IEnumerable<string> codes, CancellationToken ct);
    Task<IReadOnlyList<ClassificationDefinition>> ListClassificationDefinitionsAsync(CancellationToken ct);
    Task<IReadOnlyList<OrganizationCorrectionRequest>> ListCorrectionsAsync(Guid organizationId, CancellationToken ct);
    Task AddOrganizationAsync(Organization organization, CancellationToken ct);
    Task AddApplicationAsync(RegistrationApplication application, CancellationToken ct);
    Task AddAccessGrantAsync(OrganizationAccessGrant grant, CancellationToken ct);
    Task AddCorrectionAsync(OrganizationCorrectionRequest correction, CancellationToken ct);
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRegistrationNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct);
}