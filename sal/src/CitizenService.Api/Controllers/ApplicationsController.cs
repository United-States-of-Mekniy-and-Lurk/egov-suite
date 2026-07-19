using System.Text.Json;
using CitizenService.Application.Interfaces;
using CitizenService.Application.Services;
using CitizenService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("citizenship-applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationAppService _applicationService;
    private readonly ICurrentActor _currentActor;

    public ApplicationsController(ApplicationAppService applicationService, ICurrentActor currentActor)
    {
        _applicationService = applicationService;
        _currentActor = currentActor;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? personId,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        if (personId.HasValue)
        {
            if (personId.Value != _currentActor.PersonId && !IsStaff())
                return Forbid();

            var byPerson = await _applicationService.ListByPersonIdAsync(personId.Value, ct);
            return Ok(byPerson);
        }

        if (!IsStaff()) return Forbid();

        var applications = await _applicationService.ListAsync(status, skip, take, ct);
        return Ok(applications);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        if (request.PersonId != _currentActor.PersonId && !IsAdmin())
            return Forbid();

        var application = await _applicationService.CreateDraftAsync(request.PersonId, request.FormName, request.FormVersion, ct);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(id, ct);
        if (application == null) return NotFound();
        if (application.PersonId != _currentActor.PersonId && !IsStaff()) return Forbid();
        return Ok(application);
    }

    [HttpPut("{id:guid}/answers")]
    public async Task<IActionResult> SaveAnswers(Guid id, [FromBody] SaveAnswersRequest request, CancellationToken ct)
    {
        var existing = await _applicationService.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();
        if (existing.PersonId != _currentActor.PersonId && !IsStaff()) return Forbid();

        var application = await _applicationService.SaveAnswersAsync(id, request.Answers, ct);
        return Ok(application);
    }

    [HttpPost("{id:guid}/transition")]
    public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionRequest request, CancellationToken ct)
    {
        var existing = await _applicationService.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        var isApplicantTransition = request.TargetState is ApplicationStatus.Submitted or ApplicationStatus.Withdrawn;
        if (isApplicantTransition)
        {
            if (existing.PersonId != _currentActor.PersonId) return Forbid();
        }
        else if (!IsStaff())
        {
            return Forbid();
        }

        try
        {
            var application = await _applicationService.TransitionAsync(id, request.TargetState, request.Reason, ct);
            return Ok(application);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    private bool IsStaff() =>
        _currentActor.IsInRole("citizen-service:clerk") || IsAdmin();

    private bool IsAdmin() => _currentActor.IsInRole("citizen-service:admin");
}

public class CreateApplicationRequest
{
    public Guid PersonId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public int FormVersion { get; set; }
}

public class SaveAnswersRequest
{
    public JsonDocument Answers { get; set; } = JsonDocument.Parse("{}");
}

public class TransitionRequest
{
    public ApplicationStatus TargetState { get; set; }
    public string? Reason { get; set; }
}
