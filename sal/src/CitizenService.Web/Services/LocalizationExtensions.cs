using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Services;

public static class LocalizationExtensions
{
    public static string StatusLabel(this IStringLocalizer localizer, string? status) => status switch
    {
        "Draft" => localizer["applications.status.draft"].Value,
        "Submitted" => localizer["applications.status.submitted"].Value,
        "UnderReview" => localizer["applications.status.under_review"].Value,
        "Approved" => localizer["applications.status.approved"].Value,
        "Rejected" => localizer["applications.status.rejected"].Value,
        "Withdrawn" => localizer["applications.status.withdrawn"].Value,
        "Active" => localizer["common.active"].Value,
        "Inactive" => localizer["common.inactive"].Value,
        _ => status ?? string.Empty
    };
}