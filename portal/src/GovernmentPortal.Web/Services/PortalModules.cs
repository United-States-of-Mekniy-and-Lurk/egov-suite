using System.Globalization;
using Microsoft.Extensions.Localization;

namespace GovernmentPortal.Web.Services;

public interface IPortalModule
{
    Task<PortalModuleView> GetAsync(CancellationToken cancellationToken);
}

public sealed record PortalModuleView(
    string Title,
    string Summary,
    string ActionLabel,
    string ActionUrl,
    string Tone);

public sealed class CitizenshipPortalModule(ServiceCatalog catalog, IStringLocalizer localizer) : IPortalModule
{
    public Task<PortalModuleView> GetAsync(CancellationToken cancellationToken)
    {
        var service = catalog.Find("citizen-service");
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var serviceName = service?.GetLocalization(culture).Name ?? localizer["portal.module.citizenship.title"];
        return Task.FromResult(new PortalModuleView(
            serviceName,
            localizer["portal.module.citizenship.summary"],
            localizer["portal.module.citizenship.action"],
            service?.Url ?? "#",
            "blue"));
    }
}