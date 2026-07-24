using System.Text.RegularExpressions;
using Egov.Platform.Identity;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Services;

public sealed partial class HistoricalOrganizationService(
    IOrganizationRegistryStore store,
    ICurrentActor actor)
{
    public async Task<PublicOrganizationView> CreateAsync(CreateHistoricalOrganizationInput input, CancellationToken ct)
    {
        EnsureStaff();
        Validate(input);

        var registrationNumber = input.RegistrationNumber.Trim();
        if (await store.RegistrationNumberExistsAsync(registrationNumber, ct))
            throw new RegistryConflictException("An organization with this registration number already exists.");

        var classificationCodes = NormalizeCodes(input.ClassificationCodes);
        var definitions = await store.GetClassificationsAsync(classificationCodes, ct);
        if (definitions.Count != classificationCodes.Length)
            throw new RegistryValidationException("One or more classification codes are invalid.");

        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = registrationNumber,
            Slug = await CreateSlugAsync(input.LegalName, ct),
            LegalName = input.LegalName.Trim(),
            TradingName = NullIfWhiteSpace(input.TradingName),
            LegalFormCode = input.LegalFormCode.Trim(),
            Purpose = input.Purpose.Trim(),
            RegisteredAddress = input.RegisteredAddress.Trim(),
            RegisteredAt = input.RegisteredOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EstablishedOn = input.EstablishedOn,
            ImportSourceReference = input.SourceReference.Trim(),
            ImportNote = NullIfWhiteSpace(input.ImportNote),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByPersonId = actor.PersonId
        };
        organization.Classifications.AddRange(definitions.Select(definition => new OrganizationClassification
        {
            OrganizationId = organization.Id,
            DefinitionId = definition.Id,
            Definition = definition,
            AssignedAt = now,
            AssignedByPersonId = actor.PersonId
        }));

        await store.AddOrganizationAsync(organization, ct);
        if (input.OwnerPersonId.HasValue)
        {
            await store.AddAccessGrantAsync(new OrganizationAccessGrant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                PersonId = input.OwnerPersonId.Value,
                RoleCode = OrganizationAccessRoles.Owner,
                ValidFrom = now,
                GrantedByPersonId = actor.PersonId
            }, ct);
        }

        await store.SaveChangesAsync(ct);
        return organization.ToPublicView();
    }

    private async Task<string> CreateSlugAsync(string legalName, CancellationToken ct)
    {
        var slugBase = SlugRegex().Replace(legalName.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "organization";
        var slug = slugBase;
        var suffix = 2;
        while (await store.SlugExistsAsync(slug, ct)) slug = $"{slugBase}-{suffix++}";
        return slug;
    }

    private void EnsureStaff()
    {
        if (actor.PersonId == Guid.Empty ||
            (!actor.IsInRole("organization-registry:clerk") && !actor.IsInRole("organization-registry:admin")))
            throw new RegistryForbiddenException("A registry staff role is required.");
    }

    private static void Validate(CreateHistoricalOrganizationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.RegistrationNumber))
            throw new RegistryValidationException("Registration number is required.");
        if (input.RegistrationNumber.Trim().Length > 32)
            throw new RegistryValidationException("Registration number cannot exceed 32 characters.");
        if (input.RegisteredOn > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new RegistryValidationException("Registration date cannot be in the future.");
        if (input.EstablishedOn > input.RegisteredOn)
            throw new RegistryValidationException("Establishment date cannot be after the registration date.");
        if (string.IsNullOrWhiteSpace(input.SourceReference))
            throw new RegistryValidationException("A source reference is required.");
        if (input.OwnerPersonId == Guid.Empty)
            throw new RegistryValidationException("Owner person ID must be a valid identifier.");
        if (string.IsNullOrWhiteSpace(input.LegalName))
            throw new RegistryValidationException("Legal name is required.");
        if (string.IsNullOrWhiteSpace(input.LegalFormCode))
            throw new RegistryValidationException("Legal form is required.");
        if (string.IsNullOrWhiteSpace(input.Purpose))
            throw new RegistryValidationException("Purpose is required.");
        if (string.IsNullOrWhiteSpace(input.RegisteredAddress))
            throw new RegistryValidationException("Registered address is required.");
    }

    private static string[] NormalizeCodes(IEnumerable<string>? codes) =>
        (codes ?? []).Select(code => code.Trim().ToLowerInvariant()).Where(code => code.Length > 0).Distinct().ToArray();

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}