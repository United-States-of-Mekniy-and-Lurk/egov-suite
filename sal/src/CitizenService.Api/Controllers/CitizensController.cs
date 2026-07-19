using System.Globalization;
using CitizenService.Application.Services;
using CitizenService.Domain.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("citizens")]
[Authorize]
public class CitizensController : ControllerBase
{
    private readonly CitizenAppService _citizenService;

    public CitizensController(CitizenAppService citizenService)
    {
        _citizenService = citizenService;
    }

    [HttpGet("{personId:guid}")]
    public async Task<IActionResult> GetByPersonId(Guid personId, CancellationToken ct)
    {
        var citizen = await _citizenService.GetByPersonIdAsync(personId, ct);
        if (citizen == null) return NotFound();
        return Ok(citizen);
    }

    [HttpGet]
    [Authorize(Policy = "RequireClerk")]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var citizens = await _citizenService.ListAsync(skip, take, ct);
        return Ok(citizens);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCitizenRequest request, CancellationToken ct)
    {
        var citizen = await _citizenService.CreateCitizenAsync(request.PersonId, request.GrantedAt, request.ImportSource, request.CitizenNumber, ct);
        return CreatedAtAction(nameof(GetByPersonId), new { personId = citizen.PersonId }, citizen);
    }

    [HttpPatch("{personId:guid}/status")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ChangeStatus(Guid personId, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var citizen = await _citizenService.ChangeStatusAsync(personId, request.Status, request.Reason, ct);
        return Ok(citizen);
    }

    [HttpPost("import/csv")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var results = new List<object>();
        var errors = new List<string>();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            try
            {
                var personIdStr = csv.GetField("PersonId") ?? string.Empty;
                var citizenNumber = csv.GetField("CitizenNumber");
                var grantedAtStr = csv.GetField("GrantedAt");
                var importSource = csv.GetField("ImportSource");

                if (!Guid.TryParse(personIdStr, out var personId))
                {
                    errors.Add($"Row {csv.Context.Parser?.Row}: Invalid PersonId '{personIdStr}'");
                    continue;
                }

                DateTime? grantedAt = null;
                if (!string.IsNullOrWhiteSpace(grantedAtStr) && DateTime.TryParse(grantedAtStr, out var parsedDate))
                    grantedAt = parsedDate;

                var citizen = await _citizenService.CreateCitizenAsync(personId, grantedAt, importSource, citizenNumber, ct);
                results.Add(citizen);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {csv.Context.Parser?.Row}: {ex.Message}");
            }
        }

        return Ok(new { Imported = results.Count, Errors = errors });
    }
}

public class CreateCitizenRequest
{
    public Guid PersonId { get; set; }
    public DateTime? GrantedAt { get; set; }
    public string? ImportSource { get; set; }
    public string? CitizenNumber { get; set; }
}

public class ChangeStatusRequest
{
    public CitizenStatus Status { get; set; }
    public string? Reason { get; set; }
}
