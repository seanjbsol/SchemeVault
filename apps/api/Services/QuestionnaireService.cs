using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class QuestionnaireService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;

    public QuestionnaireService(AppDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<QuestionnaireTemplateDto>> ListTemplatesAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var latest = await _db.QuestionnaireResponses
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);

        return QuestionnaireCatalogue.All.Select(def =>
        {
            var last = latest.FirstOrDefault(r =>
                r.SchemeCode.Equals(def.SchemeCode, StringComparison.OrdinalIgnoreCase));
            return ToTemplateDto(def, last, includeQuestions: false);
        }).ToList();
    }

    public async Task<QuestionnaireTemplateDto?> GetTemplateAsync(string schemeCode, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var def = QuestionnaireCatalogue.Find(schemeCode);
        if (def is null)
        {
            return null;
        }

        var last = await _db.QuestionnaireResponses
            .Where(r => r.SchemeCode.ToLower() == def.SchemeCode.ToLower())
            .OrderByDescending(r => r.UpdatedAt)
            .FirstOrDefaultAsync(ct);
        return ToTemplateDto(def, last, includeQuestions: true);
    }

    public async Task<IReadOnlyList<QuestionnaireResponseDto>> ListResponsesAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var items = await _db.QuestionnaireResponses.OrderByDescending(r => r.UpdatedAt).ToListAsync(ct);
        return items.Select(item => ToResponseDto(item)).ToList();
    }

    public async Task<QuestionnaireResponseDto?> GetResponseAsync(Guid id, CancellationToken ct)
    {
        var item = await LoadAsync(id, ct);
        return item is null ? null : ToResponseDto(item, includeMarkdown: true);
    }

    public async Task<QuestionnaireResponseDto> SaveAsync(string schemeCode, QuestionnaireAnswerRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var def = QuestionnaireCatalogue.Find(schemeCode)
                  ?? throw new InvalidOperationException("Unknown scheme questionnaire.");
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
                     ?? throw new TenantAccessException();

        var answers = request.Answers ?? new Dictionary<string, string?>();
        ValidateRequired(def, answers);

        var now = DateTimeOffset.UtcNow;
        var item = new QuestionnaireResponse
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            SchemeCode = def.SchemeCode,
            AnswersJson = JsonSerializer.Serialize(answers, Json),
            Status = QuestionnaireStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Generate)
        {
            item.GeneratedMarkdown = QuestionnaireDocument.Render(def, tenant.Name, answers, now);
            item.Status = QuestionnaireStatus.Generated;
        }

        _db.QuestionnaireResponses.Add(item);
        await _db.SaveChangesAsync(ct);
        return ToResponseDto(item, includeMarkdown: true);
    }

    public async Task<(byte[] Bytes, string FileName)?> PdfAsync(Guid id, CancellationToken ct)
    {
        var item = await LoadAsync(id, ct);
        if (item is null || string.IsNullOrWhiteSpace(item.GeneratedMarkdown))
        {
            return null;
        }

        var def = QuestionnaireCatalogue.Find(item.SchemeCode);
        var title = $"{def?.SchemeName ?? item.SchemeCode} pack draft";
        var bytes = SimplePdfWriter.FromPlainText(title, item.GeneratedMarkdown);
        var fileName = $"{item.SchemeCode.ToLowerInvariant()}-pack-draft.pdf";
        return (bytes, fileName);
    }

    public async Task<(byte[] Bytes, string FileName)> ExportPackAsync(string[]? schemeCodes, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
                     ?? throw new TenantAccessException();

        var wanted = (schemeCodes is { Length: > 0 }
                ? schemeCodes
                : QuestionnaireCatalogue.All.Select(d => d.SchemeCode).ToArray())
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        var responses = await _db.QuestionnaireResponses
            .Where(r => r.Status == QuestionnaireStatus.Generated && r.GeneratedMarkdown != null)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(ct);

        var sections = new List<string>();
        foreach (var code in wanted)
        {
            var latest = responses.FirstOrDefault(r => r.SchemeCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (latest is null || string.IsNullOrWhiteSpace(latest.GeneratedMarkdown))
            {
                continue;
            }

            TenantGuard.EnsureOwns(_tenant, latest);
            sections.Add(latest.GeneratedMarkdown);
        }

        if (sections.Count == 0)
        {
            throw new InvalidOperationException(
                "Generate at least one scheme pack before exporting. Complete a questionnaire first.");
        }

        var combined = string.Join("\n\n---\n\n", sections);
        var header =
            $"# Multi-scheme compliance pack draft\n\n**Organisation:** {tenant.Name}\n\n{QuestionnaireCatalogue.Disclaimer}\n\n";
        var bytes = SimplePdfWriter.FromPlainText("SchemeVault multi-scheme pack draft", header + combined);
        return (bytes, "schemevault-multi-scheme-pack.pdf");
    }

    private async Task<QuestionnaireResponse?> LoadAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.QuestionnaireResponses.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        return item;
    }

    private static void ValidateRequired(QuestionnaireDefinition def, Dictionary<string, string?> answers)
    {
        var missing = def.Questions
            .Where(q => q.Required && (!answers.TryGetValue(q.Id, out var value) || string.IsNullOrWhiteSpace(value)))
            .Select(q => q.Prompt)
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException("Please answer: " + string.Join("; ", missing.Take(3)) +
                                                (missing.Count > 3 ? "…" : string.Empty));
        }
    }

    private static QuestionnaireTemplateDto ToTemplateDto(
        QuestionnaireDefinition def,
        QuestionnaireResponse? latest,
        bool includeQuestions) =>
        new()
        {
            SchemeCode = def.SchemeCode,
            SchemeName = def.SchemeName,
            Title = def.Title,
            Introduction = def.Introduction,
            QuestionCount = def.Questions.Count,
            LatestResponseId = latest?.Id,
            LatestGeneratedAt = latest is { Status: QuestionnaireStatus.Generated } ? latest.UpdatedAt : null,
            Questions = includeQuestions ? def.Questions : []
        };

    private static QuestionnaireResponseDto ToResponseDto(QuestionnaireResponse item, bool includeMarkdown = false)
    {
        Dictionary<string, string?> answers;
        try
        {
            answers = JsonSerializer.Deserialize<Dictionary<string, string?>>(item.AnswersJson, Json)
                      ?? new Dictionary<string, string?>();
        }
        catch (JsonException)
        {
            answers = new Dictionary<string, string?>();
        }

        var def = QuestionnaireCatalogue.Find(item.SchemeCode);
        return new QuestionnaireResponseDto
        {
            Id = item.Id,
            SchemeCode = item.SchemeCode,
            SchemeName = def?.SchemeName ?? item.SchemeCode,
            Status = item.Status.ToString(),
            Answers = answers,
            GeneratedMarkdown = includeMarkdown ? item.GeneratedMarkdown : null,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
