using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Billing;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Domain;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/photos")]
public sealed class PhotosController : ControllerBase
{
    private readonly PhotoService _photos;

    public PhotosController(PhotoService photos)
    {
        _photos = photos;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PhotoDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await _photos.GetAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await _photos.OpenAsync(id, ct);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Stream, file.Value.ContentType, file.Value.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _photos.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}

[ApiController]
[Authorize]
[Route("api")]
public sealed class PhotoUploadController : ControllerBase
{
    private readonly PhotoService _photos;

    public PhotoUploadController(PhotoService photos)
    {
        _photos = photos;
    }

    [HttpGet("evidence/{id:guid}/photos")]
    public Task<ActionResult<IReadOnlyList<PhotoDto>>> EvidencePhotos(Guid id, CancellationToken ct) =>
        List(PhotoOwnerKind.Evidence, id, ct);

    [HttpPost("evidence/{id:guid}/photos")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<ActionResult<PhotoDto>> UploadEvidence(Guid id, IFormFile file, [FromForm] string? caption, CancellationToken ct) =>
        Upload(PhotoOwnerKind.Evidence, id, file, caption, ct);

    [RequirePro(PlanFeatures.Accidents)]
    [HttpGet("accidents/{id:guid}/photos")]
    public Task<ActionResult<IReadOnlyList<PhotoDto>>> AccidentPhotos(Guid id, CancellationToken ct) =>
        List(PhotoOwnerKind.Accident, id, ct);

    [RequirePro(PlanFeatures.Accidents)]
    [HttpPost("accidents/{id:guid}/photos")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<ActionResult<PhotoDto>> UploadAccident(Guid id, IFormFile file, [FromForm] string? caption, CancellationToken ct) =>
        Upload(PhotoOwnerKind.Accident, id, file, caption, ct);

    [RequirePro(PlanFeatures.Equipment)]
    [HttpGet("equipment/{id:guid}/photos")]
    public Task<ActionResult<IReadOnlyList<PhotoDto>>> EquipmentPhotos(Guid id, CancellationToken ct) =>
        List(PhotoOwnerKind.Equipment, id, ct);

    [RequirePro(PlanFeatures.Equipment)]
    [HttpPost("equipment/{id:guid}/photos")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<ActionResult<PhotoDto>> UploadEquipment(Guid id, IFormFile file, [FromForm] string? caption, CancellationToken ct) =>
        Upload(PhotoOwnerKind.Equipment, id, file, caption, ct);

    private async Task<ActionResult<IReadOnlyList<PhotoDto>>> List(PhotoOwnerKind kind, Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _photos.ListAsync(kind, id, ct));
        }
        catch (TenantAccessException)
        {
            return NotFound();
        }
    }

    private async Task<ActionResult<PhotoDto>> Upload(PhotoOwnerKind kind, Guid id, IFormFile file, string? caption, CancellationToken ct)
    {
        try
        {
            var dto = await _photos.AttachAsync(kind, id, file, caption, ct);
            return Created($"/api/photos/{dto.Id}", dto);
        }
        catch (TenantAccessException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }
}
