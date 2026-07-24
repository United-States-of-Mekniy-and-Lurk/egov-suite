using Egov.Platform.Identity;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Services;

public sealed class CorrectionService(
    IOrganizationRegistryStore store,
    OrganizationQueryService organizationQueries,
    ICurrentActor actor)
{
    private static readonly IReadOnlySet<string> CorrectionRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        OrganizationAccessRoles.Owner,
        OrganizationAccessRoles.Administrator,
        OrganizationAccessRoles.Representative
    };

    public async Task<IReadOnlyList<CorrectionView>> ListAsync(Guid organizationId, CancellationToken ct)
    {
        await organizationQueries.EnsureAccessAsync(organizationId, CorrectionRoles, ct);
        return (await store.ListCorrectionsAsync(organizationId, ct)).Select(ToView).ToList();
    }

    public async Task<CorrectionView> CreateAsync(Guid organizationId, CreateCorrectionInput input, CancellationToken ct)
    {
        await organizationQueries.EnsureAccessAsync(organizationId, CorrectionRoles, ct);
        if (string.IsNullOrWhiteSpace(input.FieldKey) || string.IsNullOrWhiteSpace(input.Reason))
            throw new RegistryValidationException("A field and reason are required.");
        var organization = await store.GetOrganizationAsync(organizationId, ct)
            ?? throw new RegistryNotFoundException("Organization not found.");
        var currentValue = input.FieldKey switch
        {
            "legalName" => organization.LegalName,
            "tradingName" => organization.TradingName,
            "purpose" => organization.Purpose,
            "registeredAddress" => organization.RegisteredAddress,
            _ => throw new RegistryValidationException("This field cannot be corrected through the standard process.")
        };
        var correction = new OrganizationCorrectionRequest
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RequestedByPersonId = actor.PersonId,
            FieldKey = input.FieldKey,
            CurrentValue = currentValue,
            ProposedValue = input.ProposedValue?.Trim(),
            Reason = input.Reason.Trim(),
            SubmittedAt = DateTime.UtcNow
        };
        await store.AddCorrectionAsync(correction, ct);
        await store.SaveChangesAsync(ct);
        return ToView(correction);
    }

    private static CorrectionView ToView(OrganizationCorrectionRequest correction) => new(
        correction.Id,
        correction.OrganizationId,
        correction.FieldKey,
        correction.CurrentValue,
        correction.ProposedValue,
        correction.Reason,
        correction.Status,
        correction.SubmittedAt,
        correction.ReviewReason);
}