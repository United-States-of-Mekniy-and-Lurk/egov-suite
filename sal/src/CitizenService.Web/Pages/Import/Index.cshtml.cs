using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace CitizenService.Web.Pages.Import;

[Authorize(Policy = "RequireAdmin")]
public class ImportIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStringLocalizer _localizer;

    public ImportResult? ImportResult { get; set; }

    public ImportIndexModel(IHttpClientFactory httpClientFactory, IStringLocalizer localizer)
    {
        _httpClientFactory = httpClientFactory;
        _localizer = localizer;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", _localizer["import.file_required"].Value);
            return Page();
        }

        var client = _httpClientFactory.CreateClient("CitizenApi");
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        content.Add(fileContent, "file", file.FileName);

        var response = await client.PostAsync("/citizens/import/csv", content, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ImportResult>(cancellationToken: ct);
            ImportResult = result;
        }

        return Page();
    }
}

public class ImportResult
{
    public int Imported { get; set; }
    public List<string> Errors { get; set; } = new();
}
