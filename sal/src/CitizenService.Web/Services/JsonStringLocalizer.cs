using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Services;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private readonly string _translationsPath;

    public JsonStringLocalizer(string translationsPath)
    {
        _translationsPath = translationsPath;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = Resolve(name);
            return value != null
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Resolve(name);
            if (value != null)
            {
                var formatted = string.Format(value, arguments);
                return new LocalizedString(name, formatted);
            }
            return new LocalizedString(name, string.Format(name, arguments), resourceNotFound: true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var translations = LoadTranslations(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        foreach (var kvp in translations)
            yield return new LocalizedString(kvp.Key, kvp.Value);
    }

    private string? Resolve(string name)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var translations = LoadTranslations(culture);
        if (translations.TryGetValue(name, out var value))
            return value;

        // Fall back to English
        if (culture != "en")
        {
            translations = LoadTranslations("en");
            if (translations.TryGetValue(name, out value))
                return value;
        }

        return null;
    }

    private Dictionary<string, string> LoadTranslations(string culture)
    {
        return _cache.GetOrAdd(culture, lang =>
        {
            var filePath = Path.Combine(_translationsPath, $"{lang}.json");
            if (!File.Exists(filePath))
                return new Dictionary<string, string>();

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        });
    }
}

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly JsonStringLocalizer _localizer;

    public JsonStringLocalizerFactory(string translationsPath)
    {
        _localizer = new JsonStringLocalizer(translationsPath);
    }

    public IStringLocalizer Create(Type resourceSource) => _localizer;
    public IStringLocalizer Create(string baseName, string location) => _localizer;
}
