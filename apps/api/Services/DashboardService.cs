using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class DashboardService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;

    public DashboardService(AppDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var soon = now.AddDays(TrafficLights.AmberWindowDays);

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
                     ?? throw new TenantAccessException();

        var renewals = await _db.Accreditations.Include(a => a.Scheme).ToListAsync(ct);
        foreach (var r in renewals)
        {
            TenantGuard.EnsureOwns(_tenant, r);
        }

        var expired = renewals
            .Where(r => TrafficLights.ForAccreditation(r.Status, r.ExpiresOn, now) == TrafficLight.Red)
            .OrderBy(r => r.ExpiresOn)
            .Select(r => r.ToDto(now))
            .ToList();

        var upcoming = renewals
            .Where(r => r.ExpiresOn is not null && r.ExpiresOn >= now && r.ExpiresOn <= soon)
            .OrderBy(r => r.ExpiresOn)
            .Select(r => r.ToDto(now))
            .ToList();

        var missing = await _db.GapChecklistItems
            .Include(g => g.EvidenceItem)
            .Where(g => g.Status == GapItemStatus.Missing)
            .OrderBy(g => g.Title)
            .Take(12)
            .ToListAsync(ct);
        foreach (var g in missing)
        {
            TenantGuard.EnsureOwns(_tenant, g);
        }

        var missingCount = await _db.GapChecklistItems.CountAsync(g => g.Status == GapItemStatus.Missing, ct);
        var evidenceCount = await _db.EvidenceItems.CountAsync(ct);
        var openAccidents = await _db.Accidents.CountAsync(a => a.Status == AccidentStatus.Open, ct);
        var yearAgo = now.AddYears(-1);
        var lostHours = await _db.LostHours
            .Where(h => h.OccurredOn >= yearAgo)
            .SumAsync(h => (decimal?)h.Hours, ct) ?? 0;
        var equipment = await _db.Equipment.ToListAsync(ct);
        var overdueEquipment = equipment.Count(e => EquipmentService.IsOverdue(e, now));
        var active = renewals.Count(r =>
            TrafficLights.DerivedStatus(r.ExpiresOn, now, r.Status) is AccreditationStatus.Active
                or AccreditationStatus.ExpiringSoon);

        return new DashboardDto
        {
            TenantName = tenant.Name,
            Counts = new DashboardCountsDto
            {
                Upcoming = upcoming.Count,
                Expired = expired.Count,
                MissingEvidence = missingCount,
                ActiveSchemes = active,
                EvidenceItems = evidenceCount,
                OpenAccidents = openAccidents,
                OverdueEquipment = overdueEquipment,
                LostHoursLast12Months = lostHours
            },
            UpcomingRenewals = upcoming,
            ExpiredRenewals = expired,
            MissingEvidence = missing.Select(g => g.ToDto()).ToList()
        };
    }
}
