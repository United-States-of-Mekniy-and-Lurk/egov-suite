using System.Text.RegularExpressions;
using Egov.Platform.Identity;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Domain.Entities;
using OrganizationRegistry.Domain.Enums;
using OrganizationRegistry.Domain.StateMachine;

namespace OrganizationRegistry.Application.Services;

public sealed partial class RegistrationApplicationService(
    IOrganizationRegistryStore store,
    IRegistrationNumberGenerator numberGenerator,
    ICurrentActor actor)
{
    public async Task<RegistrationApplicationView> CreateDraftAsync(CreateRegistrationInput input, CancellationToken ct)
    {
        EnsurePerson();
        Validate(input.LegalName, input.LegalFormCode, input.Purpose, input.RegisteredAddress);
        var now = DateTime.UtcNow;
        var application = new RegistrationApplication
        {
            Id = Guid.NewGuid(),
            ApplicantPersonId = actor.PersonId,
            LegalName = input.LegalName.Trim(),
            TradingName = NullIfWhiteSpace(input.TradingName),
            LegalFormCode = input.LegalFormCode.Trim(),
            Purpose = input.Purpose.Trim(),
            RegisteredAddress = input.RegisteredAddress.Trim(),
            RequestedClassificationCodes = NormalizeCodes(input.ClassificationCodes),
            CreatedAt = now,
            UpdatedAt = now
        };
        await store.AddApplicationAsync(application, ct);
        await store.SaveChangesAsync(ct);
        return application.ToView();
    }

    public async Task<IReadOnlyList<RegistrationApplicationView>> ListMineAsync(CancellationToken ct)
    {
        EnsurePerson();
        return (await store.ListApplicationsForPersonAsync(actor.PersonId, ct)).Select(item => item.ToView()).ToList();
    }

    public async Task<IReadOnlyList<RegistrationApplicationView>> ListQueueAsync(
        RegistrationApplicationStatus? status,
        int skip,
        int take,
        CancellationToken ct)
    {
        EnsureStaff();
        var items = await store.ListApplicationsByStatusAsync(status, Math.Max(skip, 0), Math.Clamp(take, 1, 100), ct);
        return items.Select(item => item.ToView()).ToList();
    }

    public async Task<RegistrationApplicationView> GetAsync(Guid id, CancellationToken ct)
    {
        var application = await GetRequiredAsync(id, ct);
        if (application.ApplicantPersonId != actor.PersonId && !IsStaff())
            throw new RegistryForbiddenException("You do not have access to this application.");
        return application.ToView();
    }

    public async Task<RegistrationApplicationView> UpdateDraftAsync(Guid id, UpdateRegistrationInput input, CancellationToken ct)
    {
        var application = await GetRequiredAsync(id, ct);
        if (application.ApplicantPersonId != actor.PersonId)
            throw new RegistryForbiddenException("Only the applicant can update this application.");
        if (application.Status is not (RegistrationApplicationStatus.Draft or RegistrationApplicationStatus.MoreInformationRequired))
            throw new RegistryConflictException("Only a draft or returned application can be updated.");
        Validate(input.LegalName, input.LegalFormCode, input.Purpose, input.RegisteredAddress);
        application.LegalName = input.LegalName.Trim();
        application.TradingName = NullIfWhiteSpace(input.TradingName);
        application.LegalFormCode = input.LegalFormCode.Trim();
        application.Purpose = input.Purpose.Trim();
        application.RegisteredAddress = input.RegisteredAddress.Trim();
        application.RequestedClassificationCodes = NormalizeCodes(input.ClassificationCodes);
        application.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return application.ToView();
    }

    public async Task<RegistrationApplicationView> TransitionAsync(
        Guid id,
        TransitionRegistrationInput input,
        CancellationToken ct)
    {
        EnsurePerson();
        var application = await GetRequiredAsync(id, ct);
        var applicantTransition = input.TargetStatus is RegistrationApplicationStatus.Submitted or RegistrationApplicationStatus.Withdrawn;
        if (applicantTransition)
        {
            if (application.ApplicantPersonId != actor.PersonId)
                throw new RegistryForbiddenException("Only the applicant can perform this transition.");
        }
        else
        {
            EnsureStaff();
        }

        if (!RegistrationStateMachine.IsValidTransition(application.Status, input.TargetStatus))
            throw new RegistryConflictException($"Cannot transition from {application.Status} to {input.TargetStatus}.");
        if (input.TargetStatus is RegistrationApplicationStatus.MoreInformationRequired or RegistrationApplicationStatus.Rejected &&
            string.IsNullOrWhiteSpace(input.Reason))
            throw new RegistryValidationException("A reason is required for this transition.");

        var now = DateTime.UtcNow;
        var previousStatus = application.Status;
        application.Status = input.TargetStatus;
        application.UpdatedAt = now;
        application.DecisionReason = NullIfWhiteSpace(input.Reason);
        if (input.TargetStatus == RegistrationApplicationStatus.Submitted)
            application.SubmittedAt = now;
        if (!applicantTransition)
        {
            application.ReviewerPersonId = actor.PersonId;
            application.ReviewedAt = now;
        }
        application.Transitions.Add(new RegistrationTransition
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = input.TargetStatus,
            ChangedByPersonId = actor.PersonId,
            ChangedAt = now,
            Reason = NullIfWhiteSpace(input.Reason)
        });

        if (input.TargetStatus == RegistrationApplicationStatus.Approved)
            await RegisterOrganizationAsync(application, now, ct);

        await store.SaveChangesAsync(ct);
        return application.ToView();
    }

    private async Task RegisterOrganizationAsync(RegistrationApplication application, DateTime now, CancellationToken ct)
    {
        var registrationNumber = await numberGenerator.NextAsync(ct);
        var slugBase = SlugRegex().Replace(application.LegalName.ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slugBase)) slugBase = "organization";
        var slug = slugBase;
        var suffix = 2;
        while (await store.SlugExistsAsync(slug, ct)) slug = $"{slugBase}-{suffix++}";

        var definitions = await store.GetClassificationsAsync(application.RequestedClassificationCodes, ct);
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = registrationNumber,
            Slug = slug,
            LegalName = application.LegalName,
            TradingName = application.TradingName,
            LegalFormCode = application.LegalFormCode,
            Purpose = application.Purpose,
            RegisteredAddress = application.RegisteredAddress,
            RegisteredAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByPersonId = application.ApplicantPersonId
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
        await store.AddAccessGrantAsync(new OrganizationAccessGrant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            PersonId = application.ApplicantPersonId,
            RoleCode = OrganizationAccessRoles.Owner,
            ValidFrom = now,
            GrantedByPersonId = actor.PersonId
        }, ct);
        application.OrganizationId = organization.Id;
    }

    private async Task<RegistrationApplication> GetRequiredAsync(Guid id, CancellationToken ct) =>
        await store.GetApplicationAsync(id, ct) ?? throw new RegistryNotFoundException("Registration application not found.");

    private void EnsurePerson()
    {
        if (actor.PersonId == Guid.Empty) throw new RegistryForbiddenException("A person identity is required.");
    }

    private void EnsureStaff()
    {
        EnsurePerson();
        if (!IsStaff()) throw new RegistryForbiddenException("A registry staff role is required.");
    }

    private bool IsStaff() => actor.IsInRole("organization-registry:clerk") || actor.IsInRole("organization-registry:admin");

    private static void Validate(string legalName, string legalFormCode, string purpose, string registeredAddress)
    {
        if (string.IsNullOrWhiteSpace(legalName)) throw new RegistryValidationException("Legal name is required.");
        if (string.IsNullOrWhiteSpace(legalFormCode)) throw new RegistryValidationException("Legal form is required.");
        if (string.IsNullOrWhiteSpace(purpose)) throw new RegistryValidationException("Purpose is required.");
        if (string.IsNullOrWhiteSpace(registeredAddress)) throw new RegistryValidationException("Registered address is required.");
    }

    private static string[] NormalizeCodes(IEnumerable<string>? codes) =>
        (codes ?? []).Select(code => code.Trim().ToLowerInvariant()).Where(code => code.Length > 0).Distinct().ToArray();

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}