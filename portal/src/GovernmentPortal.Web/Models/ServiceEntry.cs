namespace GovernmentPortal.Web.Models;

public sealed record ServiceEntry(
    string Service,
    string Name,
    string Description,
    string Url,
    string Category,
    IReadOnlyList<string> Keywords,
    IReadOnlyDictionary<string, ServiceEntryLocalization> Localizations,
    bool Public)
{
    public ServiceEntryLocalization GetLocalization(string culture) =>
        Localizations.TryGetValue(culture, out var localization)
            ? localization
            : new ServiceEntryLocalization(Name, Description, Category, Keywords);
}

public sealed record ServiceEntryLocalization(
    string Name,
    string Description,
    string Category,
    IReadOnlyList<string> Keywords);