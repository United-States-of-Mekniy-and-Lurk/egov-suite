using CitizenService.Application.Models;
using CitizenService.Application.Services;
using CitizenService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("correction-requests")]
[Authorize(Policy = "RequireClerk")]
public class FieldCorrectionsController : ControllerBase
{
    private readonly FieldCorrectionService _fieldCorrectionService;

    public FieldCorrectionsController(FieldCorrectionService fieldCorrectionService)
    {
        _fieldCorrectionService = fieldCorrectionService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] FieldCorrectionStatus? status = FieldCorrectionStatus.Submitted,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await _fieldCorrectionService.ListAsync(status, skip, take, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ReviewFieldCorrectionInput input,
        CancellationToken ct)
        => await ReviewAsync(() => _fieldCorrectionService.ApproveAsync(id, input, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] ReviewFieldCorrectionInput input,
        CancellationToken ct)
        => await ReviewAsync(() => _fieldCorrectionService.RejectAsync(id, input, ct));

    private async Task<IActionResult> ReviewAsync(Func<Task<FieldCorrectionRequestDto>> review)
    {
        try
        {
            return Ok(await review());
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
}