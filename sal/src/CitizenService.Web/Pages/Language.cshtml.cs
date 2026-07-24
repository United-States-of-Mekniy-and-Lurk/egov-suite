using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages;

public class LanguageModel : PageModel
{
    public IActionResult OnPost(string culture, string? returnUrl)
    {
        var supportedCulture = culture is "cs" ? "cs" : "en";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(supportedCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index")!);
    }
}