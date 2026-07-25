using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectionService.Web.Pages;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
    {
        var destination = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index");
        return Challenge(new AuthenticationProperties { RedirectUri = destination });
    }
}