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
    private readonly ICurrentActor _currentActor;

    public FormsController(IFormRepository formRepository, ICurrentActor currentActor)
    {
        _formRepository = formRepository;
        _currentActor = currentActor;
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

    [HttpGet("{name}/draft")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetDraft(string name, CancellationToken ct)
    {
        var draft = await _formRepository.GetDraftAsync(name, ct);
        return draft == null ? NotFound() : Ok(draft);
    }

    [HttpPut("{name}/draft")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SaveDraft(
        string name, [FromBody] CreateFormVersionRequest request, CancellationToken ct)
    {
        var validationError = ValidateDefinition(request.DefinitionJson);
        if (validationError != null) return BadRequest(validationError);

        var draft = await _formRepository.SaveDraftAsync(
            name, request.DefinitionJson, _currentActor.PersonId, ct);
        return Ok(draft);
    }

    [HttpPost("{name}/draft/publish")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> PublishDraft(string name, CancellationToken ct)
    {
        try
        {
            var form = await _formRepository.PublishDraftAsync(name, ct);
            return CreatedAtAction(nameof(GetForm), new { name, version = form.Version }, form);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{name}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateVersion(
        string name, [FromBody] CreateFormVersionRequest request, CancellationToken ct)
    {
        var validationError = ValidateDefinition(request.DefinitionJson);
        if (validationError != null) return BadRequest(validationError);

        var form = await _formRepository.AddVersionAsync(name, request.DefinitionJson, ct);
        return CreatedAtAction(nameof(GetForm), new { name, version = form.Version }, form);
    }

    private static string? ValidateDefinition(string definitionJson)
    {
        try
        {
            using var definition = JsonDocument.Parse(definitionJson);
            var hasLegacyFields = definition.RootElement.TryGetProperty("fields", out var fields) &&
                fields.ValueKind == JsonValueKind.Array;
            var hasFormioComponents = definition.RootElement.TryGetProperty("components", out var components) &&
                components.ValueKind == JsonValueKind.Array;
            return hasLegacyFields || hasFormioComponents
                ? null
                : "Form definition must contain a fields or components array.";
        }
        catch (JsonException)
        {
            return "Form definition must be valid JSON.";
        }
    }
}

public class CreateFormVersionRequest
{
    public string DefinitionJson { get; set; } = "{}";
}
