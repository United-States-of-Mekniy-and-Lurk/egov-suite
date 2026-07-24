using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;
using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[Authorize]
[Route("registration-applications")]
public sealed class RegistrationApplicationsController(RegistrationApplicationService applications) : ControllerBase
{
    [HttpGet("mine")]
    public Task<IReadOnlyList<RegistrationApplicationView>> Mine(CancellationToken ct) => applications.ListMineAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<RegistrationApplicationView> Get(Guid id, CancellationToken ct) => applications.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<RegistrationApplicationView>> Create(CreateRegistrationInput input, CancellationToken ct)
    {
        var application = await applications.CreateDraftAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = application.Id }, application);
    }

    [HttpPut("{id:guid}")]
    public Task<RegistrationApplicationView> Update(Guid id, UpdateRegistrationInput input, CancellationToken ct) =>
        applications.UpdateDraftAsync(id, input, ct);

    [HttpPost("{id:guid}/transitions")]
    public Task<RegistrationApplicationView> Transition(Guid id, TransitionRegistrationInput input, CancellationToken ct) =>
        applications.TransitionAsync(id, input, ct);

    [HttpGet("queue")]
    [Authorize(Policy = "RequireClerk")]
    public Task<IReadOnlyList<RegistrationApplicationView>> Queue(
        [FromQuery] RegistrationApplicationStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default) => applications.ListQueueAsync(status, skip, take, ct);
}