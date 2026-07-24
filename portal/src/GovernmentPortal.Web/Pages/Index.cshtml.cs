using System.Globalization;
using GovernmentPortal.Web.Models;
using GovernmentPortal.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GovernmentPortal.Web.Pages;

public sealed class IndexModel(ServiceCatalog catalog) : PageModel
{
    public IReadOnlyList<ServiceEntry> Services { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];

    public void OnGet()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        Services = catalog.GetPublicServices()
            .Where(service => service.Service != "government-portal")
            .ToArray();
        Categories = Services.Select(service => service.GetLocalization(culture).Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}