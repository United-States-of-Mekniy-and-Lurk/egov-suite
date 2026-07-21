using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("citizens/{personId:guid}/fields")]
[Authorize]
public class CitizenRegistryFieldsController : ControllerBase
{
    private readonly RegistryFieldService _registryFieldService;
    private readonly FieldCorrectionService _fieldCorrectionService;
    private readonly ICurrentActor _currentActor;

    public CitizenRegistryFieldsController(
        RegistryFieldService registryFieldService,
        FieldCorrectionService fieldCorrectionService,
        ICurrentActor currentActor)
    {
        _registryFieldService = registryFieldService;
        _fieldCorrectionService = fieldCorrectionService;
        _currentActor = currentActor;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid personId, CancellationToken ct)
    {
        if (personId != _currentActor.PersonId && !IsStaff())
            return Forbid();

        try
        {
            return Ok(await _registryFieldService.GetCitizenFieldsAsync(personId, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("/citizens/{personId:guid}/field-history")]
    public async Task<IActionResult> History(Guid personId, CancellationToken ct)
    {
        if (personId != _currentActor.PersonId && !IsStaff())
            return Forbid();

        try
        {
            return Ok(await _registryFieldService.GetCitizenFieldHistoryAsync(personId, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{fieldKey}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SetValue(
        Guid personId,
        string fieldKey,
        [FromBody] SetCitizenFieldRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _registryFieldService.SetCitizenFieldAsync(
                personId, fieldKey, request.Value, request.SourceApplicationId, null, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("{fieldKey}/correction-requests")]
    public async Task<IActionResult> SubmitCorrection(
        Guid personId,
        string fieldKey,
        [FromBody] SubmitFieldCorrectionInput input,
        CancellationToken ct)
    {
        if (personId != _currentActor.PersonId)
            return Forbid();

        try
        {
            return Ok(await _fieldCorrectionService.SubmitAsync(personId, fieldKey, input, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("/citizens/{personId:guid}/correction-requests")]
    public async Task<IActionResult> ListCorrections(Guid personId, CancellationToken ct)
    {
        if (personId != _currentActor.PersonId && !IsStaff())
            return Forbid();

        try
        {
            return Ok(await _fieldCorrectionService.ListForPersonAsync(personId, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool IsStaff() =>
        _currentActor.IsInRole("citizen-service:clerk") ||
        _currentActor.IsInRole("citizen-service:admin");
}

public class SetCitizenFieldRequest
{
    public string? Value { get; set; }
    public Guid? SourceApplicationId { get; set; }
}