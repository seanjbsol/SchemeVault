using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Billing;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[RequirePro(PlanFeatures.Accidents)]
[Route("api/accidents")]
public sealed class AccidentsController : ControllerBase
{
    private readonly AccidentService _accidents;

    public AccidentsController(AccidentService accidents)
    {
        _accidents = accidents;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccidentDto>>> List(CancellationToken ct) =>
        Ok(await _accidents.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccidentDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _accidents.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AccidentDto>> Create([FromBody] AccidentWriteRequest request, CancellationToken ct)
    {
        var dto = await _accidents.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccidentDto>> Update(Guid id, [FromBody] AccidentWriteRequest request, CancellationToken ct)
    {
        var dto = await _accidents.UpdateAsync(id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _accidents.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}

[ApiController]
[Authorize]
[RequirePro(PlanFeatures.Accidents)]
[Route("api/lost-hours")]
public sealed class LostHoursController : ControllerBase
{
    private readonly AccidentService _accidents;

    public LostHoursController(AccidentService accidents)
    {
        _accidents = accidents;
    }

    [HttpGet]
    public async Task<ActionResult<LostHoursSummaryDto>> List(CancellationToken ct) =>
        Ok(await _accidents.ListHoursAsync(ct));

    [HttpPost]
    public async Task<ActionResult<LostHoursDto>> Create([FromBody] LostHoursWriteRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await _accidents.AddHoursAsync(request, ct);
            return Created($"/api/lost-hours/{dto.Id}", dto);
        }
        catch (TenantAccessException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _accidents.DeleteHoursAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
