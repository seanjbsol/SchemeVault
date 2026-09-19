using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Billing;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[RequirePro(PlanFeatures.Questionnaires)]
[Route("api/questionnaires")]
public sealed class QuestionnairesController : ControllerBase
{
    private readonly QuestionnaireService _questionnaires;

    public QuestionnairesController(QuestionnaireService questionnaires)
    {
        _questionnaires = questionnaires;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuestionnaireTemplateDto>>> List(CancellationToken ct) =>
        Ok(await _questionnaires.ListTemplatesAsync(ct));

    [HttpGet("schemes/{schemeCode}")]
    public async Task<ActionResult<QuestionnaireTemplateDto>> Get(string schemeCode, CancellationToken ct)
    {
        var dto = await _questionnaires.GetTemplateAsync(schemeCode, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("responses")]
    public async Task<ActionResult<IReadOnlyList<QuestionnaireResponseDto>>> Responses(CancellationToken ct) =>
        Ok(await _questionnaires.ListResponsesAsync(ct));

    [HttpGet("responses/{id:guid}")]
    public async Task<ActionResult<QuestionnaireResponseDto>> GetResponse(Guid id, CancellationToken ct)
    {
        var dto = await _questionnaires.GetResponseAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("schemes/{schemeCode}/responses")]
    public async Task<ActionResult<QuestionnaireResponseDto>> Save(
        string schemeCode,
        [FromBody] QuestionnaireAnswerRequest request,
        CancellationToken ct)
    {
        try
        {
            var dto = await _questionnaires.SaveAsync(schemeCode, request, ct);
            return CreatedAtAction(nameof(GetResponse), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }

    [HttpGet("responses/{id:guid}/markdown")]
    public async Task<IActionResult> Markdown(Guid id, CancellationToken ct)
    {
        var dto = await _questionnaires.GetResponseAsync(id, ct);
        if (dto is null || string.IsNullOrWhiteSpace(dto.GeneratedMarkdown))
        {
            return NotFound();
        }

        return Content(dto.GeneratedMarkdown, "text/markdown; charset=utf-8");
    }

    [HttpGet("responses/{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var pdf = await _questionnaires.PdfAsync(id, ct);
        return pdf is null ? NotFound() : File(pdf.Value.Bytes, "application/pdf", pdf.Value.FileName);
    }

    [RequirePro(PlanFeatures.MultiSchemeExport)]
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] QuestionnaireExportRequest? request, CancellationToken ct)
    {
        try
        {
            var pdf = await _questionnaires.ExportPackAsync(request?.SchemeCodes, ct);
            return File(pdf.Bytes, "application/pdf", pdf.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { title = ex.Message });
        }
    }
}
