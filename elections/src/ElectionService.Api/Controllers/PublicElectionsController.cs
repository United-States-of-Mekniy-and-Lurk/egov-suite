using ElectionService.Application.Models;
using ElectionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/elections")]
public sealed class PublicElectionsController(PublicElectionService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ElectionView>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpGet("{identifier}")]
    public Task<ElectionView> Get(string identifier, CancellationToken ct) => service.GetAsync(identifier, ct);

    [HttpGet("{identifier}/record")]
    public Task<OfficialElectionRecordView> Record(string identifier, CancellationToken ct) => service.RecordAsync(identifier, ct);

    [HttpGet("{electionId:guid}/results")]
    public Task<IReadOnlyList<ResultView>> Results(Guid electionId, CancellationToken ct) => service.ResultsAsync(electionId, ct);
}