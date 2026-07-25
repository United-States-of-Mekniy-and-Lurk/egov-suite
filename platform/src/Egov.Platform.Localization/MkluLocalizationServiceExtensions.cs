using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Egov.Platform.Localization;

public static class MkluLocalizationServiceExtensions
{
    public static IServiceCollection AddMkluRequestLocalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var localizationOptions = new MkluLocalizationOptions();
        configuration.GetSection(MkluLocalizationOptions.SectionName).Bind(localizationOptions);

        if (localizationOptions.SupportedCultures.Length == 0)
        {
            throw new InvalidOperationException("At least one supported culture must be configured.");
        }

        services.AddSingleton(localizationOptions);
        services.AddSingleton<MkluCultureCookie>();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = localizationOptions.SupportedCultures
                .Select(culture => new CultureInfo(culture))
                .ToArray();

            options.DefaultRequestCulture = new RequestCulture(localizationOptions.DefaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider { CookieName = localizationOptions.CookieName },
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }

    public static IServiceCollection AddMkluJsonLocalization(
        this IServiceCollection services,
        string translationsPath)
    {
        services.AddLocalization();
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<MkluLocalizationOptions>();
            return new JsonStringLocalizer(translationsPath, options.FallbackCulture);
        });
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IStringLocalizerFactory>()
                .Create(typeof(MkluLocalizationServiceExtensions)));
        return services;
    }
}

public sealed class MkluCultureCookie(MkluLocalizationOptions options)
{
    public string SetCulture(HttpContext context, string? culture)
    {
        var selectedCulture = options.SupportedCultures.FirstOrDefault(supported =>
            string.Equals(supported, culture, StringComparison.OrdinalIgnoreCase))
            ?? options.DefaultCulture;
        var cookieDomain = GetCookieDomain(context.Request.Host.Host);

        context.Response.Cookies.Append(
            options.CookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
            new CookieOptions
            {
                Domain = cookieDomain,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(options.CookieLifetimeDays),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps || cookieDomain is not null
            });

        return selectedCulture;
    }

    private string? GetCookieDomain(string requestHost)
    {
        if (string.IsNullOrWhiteSpace(options.CookieDomain)) return null;

        var cookieHost = options.CookieDomain.TrimStart('.');
        return string.Equals(requestHost, cookieHost, StringComparison.OrdinalIgnoreCase) ||
               requestHost.EndsWith($".{cookieHost}", StringComparison.OrdinalIgnoreCase)
            ? options.CookieDomain
            : null;
    }
}