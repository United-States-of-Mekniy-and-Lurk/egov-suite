using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public")]
public sealed class PublicOrganizationsController(OrganizationQueryService queries) : ControllerBase
{
    [HttpGet("organizations")]
    public Task<IReadOnlyList<PublicOrganizationView>> List(
        [FromQuery] string? search,
        [FromQuery] string? classification,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default) => queries.ListPublicAsync(search, classification, skip, take, ct);

    [HttpGet("organizations/{identifier}")]
    public Task<PublicOrganizationView> Get(string identifier, CancellationToken ct) =>
        queries.GetPublicAsync(identifier, ct);

    [HttpGet("classifications")]
    public Task<IReadOnlyList<ClassificationView>> Classifications(CancellationToken ct) =>
        queries.ListClassificationsAsync(ct);
}