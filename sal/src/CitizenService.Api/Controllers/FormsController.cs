using CitizenService.Application.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormRepository _formRepository;

    public FormsController(IFormRepository formRepository)
    {
        _formRepository = formRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var forms = await _formRepository.ListFormsAsync(ct);
        return Ok(forms);
    }

    [HttpGet("{name}/latest")]
    public async Task<IActionResult> GetLatestForm(string name, CancellationToken ct)
    {
        var form = await _formRepository.GetLatestFormAsync(name, ct);
        if (form == null) return NotFound();
        return Ok(form);
    }

    [HttpGet("{name}/{version:int}")]
    public async Task<IActionResult> GetForm(string name, int version, CancellationToken ct)
    {
        var form = await _formRepository.GetFormAsync(name, version, ct);
        if (form == null) return NotFound();
        return Ok(form);
    }

    [HttpPost("{name}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateVersion(
        string name, [FromBody] CreateFormVersionRequest request, CancellationToken ct)
    {
        try
        {
            using var definition = JsonDocument.Parse(request.DefinitionJson);
            var hasLegacyFields = definition.RootElement.TryGetProperty("fields", out var fields) &&
                fields.ValueKind == JsonValueKind.Array;
            var hasFormioComponents = definition.RootElement.TryGetProperty("components", out var components) &&
                components.ValueKind == JsonValueKind.Array;
            if (!hasLegacyFields && !hasFormioComponents)
            {
                return BadRequest("Form definition must contain a fields or components array.");
            }
        }
        catch (JsonException)
        {
            return BadRequest("Form definition must be valid JSON.");
        }

        var form = await _formRepository.AddVersionAsync(name, request.DefinitionJson, ct);
        return CreatedAtAction(nameof(GetForm), new { name, version = form.Version }, form);
    }
}

public class CreateFormVersionRequest
{
    public string DefinitionJson { get; set; } = "{}";
}
