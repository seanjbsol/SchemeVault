using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class AccidentService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly PhotoService _photos;

    public AccidentService(AppDbContext db, ITenantProvider tenant, PhotoService photos)
    {
        _db = db;
        _tenant = tenant;
        _photos = photos;
    }

    public async Task<IReadOnlyList<AccidentDto>> ListAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var items = await _db.Accidents.OrderByDescending(a => a.OccurredOn).ToListAsync(ct);
        var counts = await _photos.CountByOwnerAsync(PhotoOwnerKind.Accident, items.Select(i => i.Id), ct);
        return items.Select(i =>
        {
            TenantGuard.EnsureOwns(_tenant, i);
            return ToDto(i, counts.GetValueOrDefault(i.Id));
        }).ToList();
    }

    public async Task<AccidentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accidents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        return ToDto(item, await _photos.CountAsync(PhotoOwnerKind.Accident, item.Id, ct));
    }

    public async Task<AccidentDto> CreateAsync(AccidentWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var item = new Accident
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            OccurredOn = request.OccurredOn,
            Location = request.Location.Trim(),
            Severity = request.Severity,
            Status = request.Status,
            Description = request.Description.Trim(),
            InjuredPerson = TrimOrNull(request.InjuredPerson),
            ImmediateAction = TrimOrNull(request.ImmediateAction),
            LostHours = request.LostHours,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Accidents.Add(item);
        if (request.LostHours > 0)
        {
            _db.LostHours.Add(new LostHoursEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                AccidentId = item.Id,
                OccurredOn = request.OccurredOn,
                Hours = request.LostHours,
                Reason = "Logged with incident",
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(item, 0);
    }

    public async Task<AccidentDto?> UpdateAsync(Guid id, AccidentWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accidents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        item.OccurredOn = request.OccurredOn;
        item.Location = request.Location.Trim();
        item.Severity = request.Severity;
        item.Status = request.Status;
        item.Description = request.Description.Trim();
        item.InjuredPerson = TrimOrNull(request.InjuredPerson);
        item.ImmediateAction = TrimOrNull(request.ImmediateAction);
        item.LostHours = request.LostHours;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(item, await _photos.CountAsync(PhotoOwnerKind.Accident, item.Id, ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Accidents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (item is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        await _photos.DeleteForOwnerAsync(PhotoOwnerKind.Accident, item.Id, ct);
        var hours = await _db.LostHours.Where(h => h.AccidentId == item.Id).ToListAsync(ct);
        _db.LostHours.RemoveRange(hours);
        _db.Accidents.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<LostHoursSummaryDto> ListHoursAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var items = await _db.LostHours.OrderByDescending(h => h.OccurredOn).ToListAsync(ct);
        foreach (var item in items)
        {
            TenantGuard.EnsureOwns(_tenant, item);
        }

        return new LostHoursSummaryDto
        {
            TotalHours = items.Sum(h => h.Hours),
            Entries = items.Select(ToHoursDto).ToList()
        };
    }

    public async Task<LostHoursDto> AddHoursAsync(LostHoursWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        if (request.AccidentId is Guid accidentId)
        {
            var accident = await _db.Accidents.FirstOrDefaultAsync(a => a.Id == accidentId, ct)
                           ?? throw new TenantAccessException();
            TenantGuard.EnsureOwns(_tenant, accident);
            accident.LostHours += request.Hours;
            accident.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var entry = new LostHoursEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            AccidentId = request.AccidentId,
            OccurredOn = request.OccurredOn,
            Hours = request.Hours,
            Reason = request.Reason.Trim(),
            Notes = TrimOrNull(request.Notes),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.LostHours.Add(entry);
        await _db.SaveChangesAsync(ct);
        return ToHoursDto(entry);
    }

    public async Task<bool> DeleteHoursAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var entry = await _db.LostHours.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (entry is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, entry);
        if (entry.AccidentId is Guid accidentId)
        {
            var accident = await _db.Accidents.FirstOrDefaultAsync(a => a.Id == accidentId, ct);
            if (accident is not null)
            {
                TenantGuard.EnsureOwns(_tenant, accident);
                accident.LostHours = Math.Max(0, accident.LostHours - entry.Hours);
                accident.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        _db.LostHours.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static AccidentDto ToDto(Accident item, int photos) => new()
    {
        Id = item.Id,
        OccurredOn = item.OccurredOn,
        Location = item.Location,
        Severity = item.Severity.ToString(),
        Status = item.Status.ToString(),
        Description = item.Description,
        InjuredPerson = item.InjuredPerson,
        ImmediateAction = item.ImmediateAction,
        LostHours = item.LostHours,
        PhotoCount = photos,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };

    private static LostHoursDto ToHoursDto(LostHoursEntry entry) => new()
    {
        Id = entry.Id,
        AccidentId = entry.AccidentId,
        OccurredOn = entry.OccurredOn,
        Hours = entry.Hours,
        Reason = entry.Reason,
        Notes = entry.Notes,
        CreatedAt = entry.CreatedAt
    };

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
