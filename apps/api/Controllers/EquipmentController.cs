using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Billing;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[RequirePro(PlanFeatures.Equipment)]
[Route("api/equipment")]
public sealed class EquipmentController : ControllerBase
{
    private readonly EquipmentService _equipment;

    public EquipmentController(EquipmentService equipment)
    {
        _equipment = equipment;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> List([FromQuery] bool? overdue, CancellationToken ct) =>
        Ok(await _equipment.ListAsync(overdue, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _equipment.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create([FromBody] EquipmentWriteRequest request, CancellationToken ct)
    {
        var dto = await _equipment.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EquipmentDto>> Update(Guid id, [FromBody] EquipmentWriteRequest request, CancellationToken ct)
    {
        var dto = await _equipment.UpdateAsync(id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _equipment.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
