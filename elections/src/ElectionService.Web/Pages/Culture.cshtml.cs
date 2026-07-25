using Egov.Platform.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages;

public sealed class CultureModel(MkluCultureCookie cultureCookie) : PageModel
{
    public IActionResult OnGet(string culture, string? returnUrl)
    {
        cultureCookie.SetCulture(HttpContext, culture);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index")!);
    }
}