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
    private readonly ICurrentActor _currentActor;

    public CitizenRegistryFieldsController(
        RegistryFieldService registryFieldService,
        ICurrentActor currentActor)
    {
        _registryFieldService = registryFieldService;
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
                personId, fieldKey, request.Value, request.SourceApplicationId, ct));
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

    private bool IsStaff() =>
        _currentActor.IsInRole("citizen-service:clerk") ||
        _currentActor.IsInRole("citizen-service:admin");
}

public class SetCitizenFieldRequest
{
    public string? Value { get; set; }
    public Guid? SourceApplicationId { get; set; }
}