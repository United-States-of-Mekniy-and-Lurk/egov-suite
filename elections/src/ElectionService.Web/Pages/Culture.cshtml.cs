using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages;

public sealed class CultureModel : PageModel
{
    public IActionResult OnGet(string culture, string? returnUrl)
    {
        if (culture is not ("en" or "cs")) culture = "en";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index")!);
    }
}