using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class EquipmentService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly PhotoService _photos;

    public EquipmentService(AppDbContext db, ITenantProvider tenant, PhotoService photos)
    {
        _db = db;
        _tenant = tenant;
        _photos = photos;
    }

    public async Task<IReadOnlyList<EquipmentDto>> ListAsync(bool? overdueOnly, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var items = await _db.Equipment.OrderBy(e => e.Name).ToListAsync(ct);
        var counts = await _photos.CountByOwnerAsync(PhotoOwnerKind.Equipment, items.Select(i => i.Id), ct);
        var dtos = items.Select(i =>
        {
            TenantGuard.EnsureOwns(_tenant, i);
            return ToDto(i, now, counts.GetValueOrDefault(i.Id));
        });
        if (overdueOnly == true)
        {
            dtos = dtos.Where(d => d.IsOverdue);
        }

        return dtos.ToList();
    }

    public async Task<EquipmentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        return ToDto(item, DateTimeOffset.UtcNow, await _photos.CountAsync(PhotoOwnerKind.Equipment, item.Id, ct));
    }

    public async Task<EquipmentDto> CreateAsync(EquipmentWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var item = new EquipmentItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            SerialNumber = TrimOrNull(request.SerialNumber),
            CalibrationDueOn = request.CalibrationDueOn,
            ServiceDueOn = request.ServiceDueOn,
            Notes = TrimOrNull(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Equipment.Add(item);
        await _db.SaveChangesAsync(ct);
        return ToDto(item, now, 0);
    }

    public async Task<EquipmentDto?> UpdateAsync(Guid id, EquipmentWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        item.Name = request.Name.Trim();
        item.Category = request.Category.Trim();
        item.SerialNumber = TrimOrNull(request.SerialNumber);
        item.CalibrationDueOn = request.CalibrationDueOn;
        item.ServiceDueOn = request.ServiceDueOn;
        item.Notes = TrimOrNull(request.Notes);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(item, item.UpdatedAt, await _photos.CountAsync(PhotoOwnerKind.Equipment, item.Id, ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.Equipment.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        await _photos.DeleteForOwnerAsync(PhotoOwnerKind.Equipment, item.Id, ct);
        _db.Equipment.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public static bool IsOverdue(EquipmentItem item, DateTimeOffset utcNow) =>
        (item.CalibrationDueOn is not null && item.CalibrationDueOn < utcNow) ||
        (item.ServiceDueOn is not null && item.ServiceDueOn < utcNow);

    private static EquipmentDto ToDto(EquipmentItem item, DateTimeOffset utcNow, int photos)
    {
        var cal = TrafficLights.ForDueDate(item.CalibrationDueOn, utcNow);
        var service = TrafficLights.ForDueDate(item.ServiceDueOn, utcNow);
        var light = TrafficLights.Combine(cal, service);
        return new EquipmentDto
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            SerialNumber = item.SerialNumber,
            CalibrationDueOn = item.CalibrationDueOn,
            ServiceDueOn = item.ServiceDueOn,
            Notes = item.Notes,
            IsOverdue = IsOverdue(item, utcNow),
            TrafficLight = light.ToString(),
            PhotoCount = photos,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
