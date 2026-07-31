using Egov.Platform.Exports;
using ElectionService.Application.Models;
using ElectionService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ElectionService.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableCors("PublicResults")]
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
        if (ExportResults.WantsCsv(Request))
        {
            await WriteResultsCsvAsync(results, ct);
            return new EmptyResult();
        }

        return Ok(results);
    }

    [HttpGet("{electionId:guid}/results.csv")]
    public async Task<IActionResult> ResultsCsv(Guid electionId, CancellationToken ct)
    {
        var results = await service.TabularResultsAsync(electionId, ct);
        await WriteResultsCsvAsync(results, ct);
        return new EmptyResult();
    }

    [HttpGet("{electionId:guid}/verify-receipt")]
    public Task<ReceiptVerificationResult> VerifyReceipt(Guid electionId, [FromQuery] string receipt, CancellationToken ct) =>
        service.VerifyReceiptAsync(electionId, receipt, ct);

    [HttpGet("{electionId:guid}/certification")]
    public Task<CertificationView> CertificationStatus(Guid electionId, CancellationToken ct) =>
        service.GetCertificationStatusAsync(electionId, ct);

    private async Task WriteResultsCsvAsync(TabularResultsView results, CancellationToken ct)
    {
        Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromMinutes(1) };
        var columns = new List<CsvColumn<TabularResultRow>>
        {
            new("ElectionId", _ => results.ElectionId),
            new("GeneratedAt", _ => results.GeneratedAt),
            new("IsLive", _ => results.IsLive),
            new("SelectionId", row => row.SelectionId),
            new("Party", row => row.PartyName),
            new("Selection", row => row.SelectionLabel),
            new("Type", row => row.SelectionType),
            new("Votes", row => row.VoteCount),
            new("Percentage", row => row.Percentage),
            new("Territory", row => row.TerritoryCode)
        };
        await ExportResults.WriteCsvAsync(Response, results.Rows, columns,
            $"results-{results.ElectionId:N}.csv", ct: ct);
    }
}