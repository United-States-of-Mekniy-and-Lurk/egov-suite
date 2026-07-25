using Egov.Platform.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GovernmentPortal.Web.Pages;

public sealed class CultureModel(MkluCultureCookie cultureCookie) : PageModel
{
    public IActionResult OnPost(string culture, string? returnUrl = null)
    {
        cultureCookie.SetCulture(HttpContext, culture);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}