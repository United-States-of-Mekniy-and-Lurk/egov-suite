using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;

namespace ElectionService.Web.Models;

public static partial class ElectionDisplay
{
    public static string Words(string value) => PascalCaseBoundary().Replace(value, " $1");

    public static string Status(string value, IStringLocalizer<SharedResource> localizer) =>
        localizer[$"Status_{value}"];

    public static string Type(string value, IStringLocalizer<SharedResource> localizer) =>
        localizer[$"Type_{value}"];

    public static string Eligibility(string mode, IStringLocalizer<SharedResource> localizer) => mode switch
    {
        "AllActiveCitizens" => localizer["Eligibility_AllActiveCitizens"],
        "SpecificVoterRoll" => localizer["Eligibility_SpecificVoterRoll"],
        _ => Words(mode)
    };

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex PascalCaseBoundary();
}