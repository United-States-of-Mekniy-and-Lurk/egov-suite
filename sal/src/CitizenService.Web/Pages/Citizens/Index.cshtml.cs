using System.Text.Json;
using CitizenService.Web.Models;
using CitizenService.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CitizenService.Web.Pages.Citizens;

[Authorize(Policy = "RequireClerk")]
public class CitizensIndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PersonDirectoryService _personDirectory;

    private static readonly string[] DefaultColumns = ["name", "citizenNumber", "status", "grantedAt"];
    private static readonly HashSet<string> AllowedColumns =
        ["name", "email", "username", "citizenNumber", "status", "grantedAt"];

    public List<CitizenListItemViewModel> Citizens { get; private set; } = [];
    public HashSet<string> Columns { get; private set; } = [];

    public CitizensIndexModel(
        IHttpClientFactory httpClientFactory,
        PersonDirectoryService personDirectory)
    {
        _httpClientFactory = httpClientFactory;
        _personDirectory = personDirectory;
    }

    public async Task OnGetAsync(string[]? columns, CancellationToken ct)
    {
        Columns = columns is { Length: > 0 }
            ? columns.Where(AllowedColumns.Contains).ToHashSet(StringComparer.Ordinal)
            : DefaultColumns.ToHashSet(StringComparer.Ordinal);

        var client = _httpClientFactory.CreateClient("CitizenApi");
        var response = await client.GetAsync("/citizens?skip=0&take=50", ct);
        if (!response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync(ct);
        var citizens = JsonSerializer.Deserialize<List<CitizenViewModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var people = await Task.WhenAll(citizens.Select(citizen => _personDirectory.GetAsync(citizen.PersonId, ct)));
        Citizens = citizens.Select((citizen, index) => new CitizenListItemViewModel
        {
            Citizen = citizen,
            Person = people[index]
        }).ToList();
    }

    public bool Shows(string column) => Columns.Contains(column);
}
