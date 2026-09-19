using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/schemes")]
public sealed class SchemesController : ControllerBase
{
    private readonly SchemeQueryService _schemes;

    public SchemesController(SchemeQueryService schemes)
    {
        _schemes = schemes;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchemeSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _schemes.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchemeDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _schemes.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPatch("{schemeId:guid}/gaps/{gapId:guid}")]
    public async Task<ActionResult<GapItemDto>> UpdateGap(
        Guid schemeId,
        Guid gapId,
        [FromBody] UpdateGapItemRequest request,
        CancellationToken ct)
    {
        try
        {
            var dto = await _schemes.UpdateGapAsync(schemeId, gapId, request, ct);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }
}
