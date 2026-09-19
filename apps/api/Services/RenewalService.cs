using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class RenewalService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;

    public RenewalService(AppDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<RenewalDto>> ListAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var items = await _db.Accreditations.Include(a => a.Scheme)
            .OrderBy(a => a.ExpiresOn)
            .ToListAsync(ct);
        return items.Select(i =>
        {
            TenantGuard.EnsureOwns(_tenant, i);
            return i.ToDto(now);
        }).ToList();
    }

    public async Task<RenewalDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accreditations.Include(a => a.Scheme).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        return item.ToDto(DateTimeOffset.UtcNow);
    }

    public async Task<RenewalDto> CreateAsync(RenewalWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        if (!await _db.Schemes.AnyAsync(s => s.Id == request.SchemeId, ct))
        {
            throw new InvalidOperationException("Unknown scheme.");
        }

        var exists = await _db.Accreditations.AnyAsync(a => a.SchemeId == request.SchemeId, ct);
        if (exists)
        {
            throw new InvalidOperationException("This organisation already has an accreditation record for that scheme.");
        }

        var now = DateTimeOffset.UtcNow;
        var item = new Accreditation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            SchemeId = request.SchemeId,
            Status = request.Status,
            ExpiresOn = request.ExpiresOn,
            LastSubmittedOn = request.LastSubmittedOn,
            MembershipNumber = request.MembershipNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Accreditations.Add(item);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(item).Reference(a => a.Scheme).LoadAsync(ct);
        return item.ToDto(now);
    }

    public async Task<RenewalDto?> UpdateAsync(Guid id, RenewalWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accreditations.Include(a => a.Scheme).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        if (item.SchemeId != request.SchemeId)
        {
            if (!await _db.Schemes.AnyAsync(s => s.Id == request.SchemeId, ct))
            {
                throw new InvalidOperationException("Unknown scheme.");
            }

            var clash = await _db.Accreditations.AnyAsync(a => a.SchemeId == request.SchemeId && a.Id != id, ct);
            if (clash)
            {
                throw new InvalidOperationException("This organisation already has an accreditation record for that scheme.");
            }

            item.SchemeId = request.SchemeId;
        }

        item.Status = request.Status;
        item.ExpiresOn = request.ExpiresOn;
        item.LastSubmittedOn = request.LastSubmittedOn;
        item.MembershipNumber = request.MembershipNumber?.Trim();
        item.Notes = request.Notes?.Trim();
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _db.Entry(item).Reference(a => a.Scheme).LoadAsync(ct);
        return item.ToDto(item.UpdatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accreditations.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        _db.Accreditations.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
