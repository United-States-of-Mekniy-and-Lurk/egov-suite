using Microsoft.AspNetCore.Mvc.RazorPages;
using OrganizationRegistry.Web.Models;
using OrganizationRegistry.Web.Services;

namespace OrganizationRegistry.Web.Pages.LegalForms;

public sealed class DetailModel(PublicRegistryClient publicRegistry) : PageModel
{
    public LegalForm? LegalForm { get; private set; }

    public async Task OnGetAsync(string code, CancellationToken ct)
    {
        var forms = await publicRegistry.ListLegalFormsAsync(ct);
        LegalForm = forms.FirstOrDefault(f => f.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}
