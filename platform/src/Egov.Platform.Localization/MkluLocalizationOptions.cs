namespace Egov.Platform.Localization;

public sealed class MkluLocalizationOptions
{
    public const string SectionName = "Localization";

    public string DefaultCulture { get; set; } = "en";
    public string FallbackCulture { get; set; } = "en";
    public string[] SupportedCultures { get; set; } = ["en", "cs"];
    public string CookieName { get; set; } = "mklu.culture";
    public string CookieDomain { get; set; } = ".mklu.org";
    public int CookieLifetimeDays { get; set; } = 365;
}