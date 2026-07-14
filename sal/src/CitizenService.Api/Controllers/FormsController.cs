using CitizenService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Api.Controllers;

[ApiController]
[Route("forms")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormRepository _formRepository;

    public FormsController(IFormRepository formRepository)
    {
        _formRepository = formRepository;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var forms = await _formRepository.ListFormsAsync(ct);
        return Ok(forms);
    }

    [HttpGet("{name}/{version:int}")]
    public async Task<IActionResult> GetForm(string name, int version, CancellationToken ct)
    {
        var form = await _formRepository.GetFormAsync(name, version, ct);
        if (form == null) return NotFound();
        return Ok(form);
    }
}
