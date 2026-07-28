using Egov.Platform.Exports;
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

    [HttpGet("calendar")]
    public Task<IReadOnlyList<ElectionCalendarEntry>> Calendar(CancellationToken ct) => service.CalendarAsync(ct);

    [HttpGet("{identifier}")]
    public Task<ElectionView> Get(string identifier, CancellationToken ct) => service.GetAsync(identifier, ct);

    [HttpGet("{identifier}/record")]
    public Task<OfficialElectionRecordView> Record(string identifier, CancellationToken ct) => service.RecordAsync(identifier, ct);

    [HttpGet("{electionId:guid}/results")]
    public Task<IReadOnlyList<ResultView>> Results(Guid electionId, CancellationToken ct) => service.ResultsAsync(electionId, ct);

    [HttpGet("{electionId:guid}/results/tabular")]
    public async Task<IActionResult> TabularResults(Guid electionId, CancellationToken ct)
    {
        var results = await service.TabularResultsAsync(electionId, ct);

        if (ExportResults.WantsCsv(Request))
        {
            var columns = new List<CsvColumn<TabularResultRow>>
            {
                new("Selection", r => r.SelectionLabel),
                new("Type", r => r.SelectionType),
                new("Votes", r => r.VoteCount),
                new("Percentage", r => r.Percentage),
                new("Territory", r => r.TerritoryCode)
            };
            await ExportResults.WriteCsvAsync(Response, results.Rows, columns,
                $"results-{electionId:N}.csv", ct: ct);
            return new EmptyResult();
        }

        return Ok(results);
    }

    [HttpGet("{electionId:guid}/verify-receipt")]
    public Task<ReceiptVerificationResult> VerifyReceipt(Guid electionId, [FromQuery] string receipt, CancellationToken ct) =>
        service.VerifyReceiptAsync(electionId, receipt, ct);

    [HttpGet("{electionId:guid}/certification")]
    public Task<CertificationView> CertificationStatus(Guid electionId, CancellationToken ct) =>
        service.GetCertificationStatusAsync(electionId, ct);
}