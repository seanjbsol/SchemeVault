using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/renewals")]
public sealed class RenewalsController : ControllerBase
{
    private readonly RenewalService _renewals;

    public RenewalsController(RenewalService renewals)
    {
        _renewals = renewals;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RenewalDto>>> List(CancellationToken ct) =>
        Ok(await _renewals.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RenewalDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _renewals.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<RenewalDto>> Create([FromBody] RenewalWriteRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await _renewals.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RenewalDto>> Update(Guid id, [FromBody] RenewalWriteRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await _renewals.UpdateAsync(id, request, ct);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _renewals.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
