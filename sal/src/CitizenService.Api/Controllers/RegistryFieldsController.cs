using CitizenService.Application.Models;
using CitizenService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("registry-fields")]
[Authorize]
public class RegistryFieldsController : ControllerBase
{
    private readonly RegistryFieldService _registryFieldService;

    public RegistryFieldsController(RegistryFieldService registryFieldService)
    {
        _registryFieldService = registryFieldService;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct)
    {
        if (includeInactive && !User.IsInRole("citizen-service:admin"))
            return Forbid();

        return Ok(await _registryFieldService.ListDefinitionsAsync(includeInactive, ct));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(
        [FromBody] RegistryFieldDefinitionInput input, CancellationToken ct)
    {
        try
        {
            var definition = await _registryFieldService.CreateDefinitionAsync(input, ct);
            return CreatedAtAction(nameof(List), definition);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] RegistryFieldDefinitionInput input, CancellationToken ct)
    {
        try
        {
            return Ok(await _registryFieldService.UpdateDefinitionAsync(id, input, ct));
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
}