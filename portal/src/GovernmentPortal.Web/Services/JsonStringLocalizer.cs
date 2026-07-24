using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace GovernmentPortal.Web.Services;

public sealed class JsonStringLocalizer(string translationsPath) : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();

    public LocalizedString this[string name] => Resolve(name) is { } value
        ? new LocalizedString(name, value)
        : new LocalizedString(name, name, resourceNotFound: true);

    public LocalizedString this[string name, params object[] arguments] => Resolve(name) is { } value
        ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments))
        : new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, name, arguments), resourceNotFound: true);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Load(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            .Select(pair => new LocalizedString(pair.Key, pair.Value));

    private string? Resolve(string name)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (Load(culture).TryGetValue(name, out var value)) return value;
        return culture != "en" && Load("en").TryGetValue(name, out value) ? value : null;
    }

    private Dictionary<string, string> Load(string culture) => _cache.GetOrAdd(culture, language =>
    {
        var path = Path.Combine(translationsPath, $"{language}.json");
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []
            : [];
    });
}