using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class SchemeQueryService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;

    public SchemeQueryService(AppDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<SchemeSummaryDto>> ListAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var schemes = await _db.Schemes.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync(ct);
        var accreditations = await _db.Accreditations.Include(a => a.Scheme).ToListAsync(ct);
        foreach (var a in accreditations)
        {
            TenantGuard.EnsureOwns(_tenant, a);
        }

        var gaps = await _db.GapChecklistItems.AsNoTracking().ToListAsync(ct);
        foreach (var g in gaps)
        {
            TenantGuard.EnsureOwns(_tenant, g);
        }

        return schemes.Select(s =>
        {
            var acc = accreditations.FirstOrDefault(a => a.SchemeId == s.Id);
            var schemeGaps = gaps.Where(g => g.SchemeId == s.Id).ToList();
            return new SchemeSummaryDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Provider = s.Provider,
                IsSsipStyle = s.IsSsipStyle,
                Accreditation = acc?.ToDto(now),
                MissingGaps = schemeGaps.Count(g => g.Status == GapItemStatus.Missing),
                CompleteGaps = schemeGaps.Count(g => g.Status == GapItemStatus.Complete)
            };
        }).ToList();
    }

    public async Task<SchemeDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var scheme = await _db.Schemes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scheme is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var acc = await _db.Accreditations.Include(a => a.Scheme).FirstOrDefaultAsync(a => a.SchemeId == id, ct);
        if (acc is not null)
        {
            TenantGuard.EnsureOwns(_tenant, acc);
        }

        var gaps = await _db.GapChecklistItems
            .Include(g => g.EvidenceItem)
            .Where(g => g.SchemeId == id)
            .OrderBy(g => g.Title)
            .ToListAsync(ct);
        foreach (var g in gaps)
        {
            TenantGuard.EnsureOwns(_tenant, g);
        }

        return new SchemeDetailDto
        {
            Id = scheme.Id,
            Code = scheme.Code,
            Name = scheme.Name,
            Provider = scheme.Provider,
            Description = scheme.Description,
            IsSsipStyle = scheme.IsSsipStyle,
            Accreditation = acc?.ToDto(now),
            Gaps = gaps.Select(g => g.ToDto()).ToList()
        };
    }

    public async Task<GapItemDto?> UpdateGapAsync(Guid schemeId, Guid gapId, UpdateGapItemRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var gap = await _db.GapChecklistItems
            .Include(g => g.EvidenceItem)
            .FirstOrDefaultAsync(g => g.Id == gapId && g.SchemeId == schemeId, ct);
        if (gap is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, gap);

        if (request.EvidenceItemId is Guid evidenceId)
        {
            var evidence = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == evidenceId, ct)
                           ?? throw new InvalidOperationException("Evidence item was not found in this organisation.");
            TenantGuard.EnsureOwns(_tenant, evidence);
            gap.EvidenceItemId = evidence.Id;
        }
        else
        {
            gap.EvidenceItemId = null;
        }

        gap.Status = request.Status;
        gap.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(gap).Reference(g => g.EvidenceItem).LoadAsync(ct);
        return gap.ToDto();
    }
}
