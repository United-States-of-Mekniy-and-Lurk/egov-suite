using System.Text.Json;
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

    public ApplicationsController(ApplicationAppService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var applications = await _applicationService.ListAsync(status, skip, take, ct);
        return Ok(applications);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var application = await _applicationService.CreateDraftAsync(request.PersonId, request.FormName, request.FormVersion, ct);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(id, ct);
        if (application == null) return NotFound();
        return Ok(application);
    }

    [HttpPut("{id:guid}/answers")]
    public async Task<IActionResult> SaveAnswers(Guid id, [FromBody] SaveAnswersRequest request, CancellationToken ct)
    {
        var application = await _applicationService.SaveAnswersAsync(id, request.Answers, ct);
        return Ok(application);
    }

    [HttpPost("{id:guid}/transition")]
    public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionRequest request, CancellationToken ct)
    {
        var application = await _applicationService.TransitionAsync(id, request.TargetState, request.Reason, ct);
        return Ok(application);
    }
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
