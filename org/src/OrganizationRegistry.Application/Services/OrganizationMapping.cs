using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Domain.Entities;
using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Application.Services;

internal static class OrganizationMapping
{
    public static PublicOrganizationView ToPublicView(this Organization organization)
    {
        var logo = organization.Assets
            .Where(asset => asset.Kind == OrganizationAssetKind.Logo && asset.Visibility == AssetVisibility.Public)
            .OrderByDescending(asset => asset.CreatedAt)
            .FirstOrDefault();

        return new PublicOrganizationView(
            organization.Id,
            organization.RegistrationNumber,
            organization.Slug,
            organization.LegalName,
            organization.TradingName,
            organization.LegalFormCode,
            organization.Purpose,
            organization.RegisteredAddress,
            organization.Status,
            organization.RegisteredAt,
            organization.EstablishedOn,
            organization.Classifications
                .OrderBy(item => item.Definition.SortOrder)
                .Select(item => new ClassificationView(
                    item.Definition.Scheme,
                    item.Definition.Code,
                    item.Definition.LabelEn,
                    item.Definition.LabelCs))
                .ToList(),
            logo == null ? null : $"/public/assets/{logo.Id}");
    }

    public static RegistrationApplicationView ToView(this RegistrationApplication application) => new(
        application.Id,
        application.ApplicantPersonId,
        application.Status,
        application.LegalName,
        application.TradingName,
        application.LegalFormCode,
        application.Purpose,
        application.RegisteredAddress,
        application.RequestedClassificationCodes,
        application.OrganizationId,
        application.SubmittedAt,
        application.ReviewedAt,
        application.DecisionReason,
        application.CreatedAt,
        application.UpdatedAt);
}