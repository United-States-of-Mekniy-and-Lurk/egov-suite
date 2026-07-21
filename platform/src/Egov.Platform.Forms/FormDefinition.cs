namespace Egov.Platform.Forms;

public class FormDefinition
{
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Titles { get; set; } = [];
    public List<FormFieldDefinition> Fields { get; set; } = [];

    public string GetTitle(string culture) => LocalizedText.Resolve(Titles, culture, Title);
}

public class FormFieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = [];
    public bool Required { get; set; }
    public List<FormFieldOption> Options { get; set; } = [];
    public string? Step { get; set; }

    public string GetLabel(string culture) => LocalizedText.Resolve(Labels, culture, Label);
}

public class FormFieldOption
{
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = [];

    public string GetLabel(string culture) => LocalizedText.Resolve(Labels, culture, Value);
}

internal static class LocalizedText
{
    public static string Resolve(IReadOnlyDictionary<string, string> values, string culture, string fallback)
    {
        if (values.TryGetValue(culture, out var localized)) return localized;
        if (values.TryGetValue("en", out var english)) return english;
        return fallback;
    }
}