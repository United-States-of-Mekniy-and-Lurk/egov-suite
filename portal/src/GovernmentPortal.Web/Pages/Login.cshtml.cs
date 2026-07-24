using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GovernmentPortal.Web.Pages;

public sealed class LoginModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(GetSafeReturnUrl(returnUrl));
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = GetSafeReturnUrl(returnUrl) },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    private string GetSafeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Overview";
}