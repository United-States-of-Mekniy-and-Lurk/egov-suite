using Ego.Api.Models;
using Ego.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ego.Api.Controllers;

[Authorize]
[ApiController]
[Route("")]
public class PersonsController(PersonRegistryService personRegistryService) : ControllerBase
{
    [HttpGet("persons/{id:guid}")]
    public async Task<ActionResult<PersonDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var person = await personRegistryService.GetByIdAsync(id, ct);
        return Ok(person.ToDto());
    }

    [HttpGet("persons/by-sub/{sub}")]
    public async Task<ActionResult<PersonDto>> GetByIdentitySubjectAsync(string sub, CancellationToken ct)
    {
        var person = await personRegistryService.GetByIdentitySubjectAsync(sub, ct);
        return Ok(person.ToDto());
    }

    [HttpPost("persons")]
    public async Task<ActionResult<PersonDto>> CreateAsync([FromBody] CreatePersonRequestDto request, CancellationToken ct)
    {
        var person = await personRegistryService.CreateAsync(request.ToCommand(), ct);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = person.Id }, person.ToDto());
    }

    [HttpPatch("persons/{id:guid}")]
    public async Task<ActionResult<PersonDto>> PatchAsync(Guid id, [FromBody] PatchPersonRequestDto request, CancellationToken ct)
    {
        var person = await personRegistryService.PatchAsync(id, request.ToCommand(), ct);
        return Ok(person.ToDto());
    }
}
