using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationRegistry.Application.Models;
using OrganizationRegistry.Application.Services;

namespace OrganizationRegistry.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route("staff/legal-forms")]
public sealed class LegalFormsController(LegalFormService legalForms) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LegalFormView>> List(CancellationToken ct) =>
        legalForms.ListAsync(activeOnly: false, ct);

    [HttpGet("{code}")]
    public async Task<ActionResult<LegalFormView>> Get(string code, CancellationToken ct)
    {
        var result = await legalForms.GetAsync(code, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public Task<LegalFormView> Create(CreateLegalFormInput input, CancellationToken ct) =>
        legalForms.CreateAsync(input, ct);

    [HttpPut("{code}")]
    public Task<LegalFormView> Update(string code, UpdateLegalFormInput input, CancellationToken ct) =>
        legalForms.UpdateAsync(code, input, ct);
}