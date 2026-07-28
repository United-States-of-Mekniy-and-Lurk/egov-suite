using System.Text.RegularExpressions;
using Egov.Platform.Identity;
using OrganizationRegistry.Application.Abstractions;
using OrganizationRegistry.Application.Exceptions;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Domain.Entities;

namespace OrganizationRegistry.Application.Services;

public sealed partial class LegalFormService(IOrganizationRegistryStore store, ICurrentActor actor)
{
    public async Task<IReadOnlyList<LegalFormView>> ListAsync(bool activeOnly, CancellationToken ct) =>
        (await store.ListLegalFormsAsync(activeOnly, ct)).Select(ToView).ToList();

    public async Task<LegalFormView?> GetAsync(string code, CancellationToken ct)
    {
        var item = await store.GetLegalFormAsync(code, ct);
        return item is null ? null : ToView(item);
    }

    public async Task<LegalFormView> CreateAsync(CreateLegalFormInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var code = NormalizeCode(input.Code);
        ValidateLabels(input.LabelEn, input.LabelCs);
        if (await store.GetLegalFormAsync(code, ct) is not null)
            throw new RegistryConflictException($"Legal form '{code}' already exists.");

        var legalForm = new LegalFormDefinition
        {
            Code = code,
            LabelEn = input.LabelEn.Trim(),
            LabelCs = input.LabelCs.Trim(),
            DescriptionEn = input.DescriptionEn?.Trim(),
            DescriptionCs = input.DescriptionCs?.Trim(),
            SortOrder = input.SortOrder
        };
        await store.AddLegalFormAsync(legalForm, ct);
        await store.SaveChangesAsync(ct);
        return ToView(legalForm);
    }

    public async Task<LegalFormView> UpdateAsync(string code, UpdateLegalFormInput input, CancellationToken ct)
    {
        EnsureAdmin();
        ValidateLabels(input.LabelEn, input.LabelCs);
        var legalForm = await store.GetLegalFormAsync(NormalizeCode(code), ct)
            ?? throw new RegistryNotFoundException("Legal form not found.");
        legalForm.LabelEn = input.LabelEn.Trim();
        legalForm.LabelCs = input.LabelCs.Trim();
        legalForm.DescriptionEn = input.DescriptionEn?.Trim();
        legalForm.DescriptionCs = input.DescriptionCs?.Trim();
        legalForm.IsActive = input.IsActive;
        legalForm.SortOrder = input.SortOrder;
        await store.SaveChangesAsync(ct);
        return ToView(legalForm);
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 80 || !CodeRegex().IsMatch(normalized))
            throw new RegistryValidationException("Legal form code must contain only letters, numbers, and hyphens.");
        return normalized;
    }

    private static void ValidateLabels(string labelEn, string labelCs)
    {
        if (string.IsNullOrWhiteSpace(labelEn) || string.IsNullOrWhiteSpace(labelCs))
            throw new RegistryValidationException("English and Czech labels are required.");
        if (labelEn.Trim().Length > 160 || labelCs.Trim().Length > 160)
            throw new RegistryValidationException("Legal form labels cannot exceed 160 characters.");
    }

    private void EnsureAdmin()
    {
        if (actor.PersonId == Guid.Empty || !actor.IsInRole("organization-registry:admin"))
            throw new RegistryForbiddenException("A registry administrator role is required.");
    }

    private static LegalFormView ToView(LegalFormDefinition item) =>
        new(item.Code, item.LabelEn, item.LabelCs, item.DescriptionEn, item.DescriptionCs, item.IsActive, item.SortOrder);

    [GeneratedRegex("^[A-Z0-9]+(?:-[A-Z0-9]+)*$")]
    private static partial Regex CodeRegex();
}