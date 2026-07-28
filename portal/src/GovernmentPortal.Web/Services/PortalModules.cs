using System.Globalization;
using System.Text.Json;
using Egov.Platform.Identity;
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
    string Tone,
    string Icon,
    string Status);

public sealed class CitizenshipPortalModule(
    ServiceCatalog catalog,
    IHttpClientFactory httpClientFactory,
    IStringLocalizer localizer,
    ILogger<CitizenshipPortalModule> logger) : IPortalModule
{
    public async Task<PortalModuleView> GetAsync(CancellationToken cancellationToken)
    {
        var service = catalog.Find("citizen-service");
        try
        {
            using var response = await httpClientFactory.CreateClient("CitizenApi")
                .GetAsync("/citizenship-applications/mine", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var applications = payload.RootElement.EnumerateArray().ToArray();
            var inProgress = applications.Count(application =>
                application.TryGetProperty("status", out var status) &&
                status.GetString() is "Draft" or "Submitted" or "UnderReview");
            return CreateView(
                service,
                localizer["portal.module.citizenship.summary_live", applications.Length, inProgress],
                localizer["portal.overview.current"].Value);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not load the citizenship overview module");
            return CreateView(
                service,
                localizer["portal.overview.unavailable_summary"],
                localizer["portal.overview.unavailable"].Value);
        }
    }

    private PortalModuleView CreateView(GovernmentPortal.Web.Models.ServiceEntry? service, string summary, string status)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new PortalModuleView(
            service?.GetLocalization(culture).Name ?? localizer["portal.module.citizenship.title"],
            summary,
            localizer["portal.module.citizenship.action"],
            service?.Url ?? "#",
            "blue",
            "file-check",
            status);
    }
}

public sealed class OrganizationPortalModule(
    ServiceCatalog catalog,
    IHttpClientFactory httpClientFactory,
    IStringLocalizer localizer,
    ILogger<OrganizationPortalModule> logger) : IPortalModule
{
    public async Task<PortalModuleView> GetAsync(CancellationToken cancellationToken)
    {
        var service = catalog.Find("organization-registry");
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var serviceName = service?.GetLocalization(culture).Name ?? localizer["portal.module.organizations.title"];
        try
        {
            using var response = await httpClientFactory.CreateClient("OrganizationApi")
                .GetAsync("/organizations/mine", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            return new PortalModuleView(
                serviceName,
                localizer["portal.module.organizations.summary_live", payload.RootElement.GetArrayLength()],
                localizer["portal.module.organizations.action"],
                service?.Url ?? "#",
                "blue",
                "building",
                localizer["portal.overview.current"]);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Could not load the organisation overview module");
            return new PortalModuleView(
                serviceName,
                localizer["portal.overview.unavailable_summary"],
                localizer["portal.module.organizations.action"],
                service?.Url ?? "#",
                "neutral",
                "building",
                localizer["portal.overview.unavailable"]);
        }
    }
}

