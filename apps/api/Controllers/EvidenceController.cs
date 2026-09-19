using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/evidence")]
public sealed class EvidenceController : ControllerBase
{
    private readonly EvidenceService _evidence;

    public EvidenceController(EvidenceService evidence)
    {
        _evidence = evidence;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EvidenceDto>>> List(CancellationToken ct) =>
        Ok(await _evidence.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _evidence.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<EvidenceDto>> Create([FromBody] EvidenceWriteRequest request, CancellationToken ct)
    {
        var dto = await _evidence.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EvidenceDto>> Update(Guid id, [FromBody] EvidenceWriteRequest request, CancellationToken ct)
    {
        var dto = await _evidence.UpdateAsync(id, request, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _evidence.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<EvidenceDto>> Upload(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { title = "Choose a file to upload." });
        }

        var dto = await _evidence.AttachFileAsync(id, file, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await _evidence.OpenFileAsync(id, ct);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
    }
}
