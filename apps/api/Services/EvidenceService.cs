using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class EvidenceService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _files;
    private readonly PhotoService _photos;

    public EvidenceService(AppDbContext db, ITenantProvider tenant, IFileStorage files, PhotoService photos)
    {
        _db = db;
        _tenant = tenant;
        _files = files;
        _photos = photos;
    }

    public async Task<IReadOnlyList<EvidenceDto>> ListAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var items = await _db.EvidenceItems.OrderByDescending(e => e.UpdatedAt).ToListAsync(ct);
        var counts = await _photos.CountByOwnerAsync(PhotoOwnerKind.Evidence, items.Select(i => i.Id), ct);
        return items.Select(i =>
        {
            TenantGuard.EnsureOwns(_tenant, i);
            return i.ToDto(now, counts.GetValueOrDefault(i.Id));
        }).ToList();
    }

    public async Task<EvidenceDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        return item.ToDto(DateTimeOffset.UtcNow, await _photos.CountAsync(PhotoOwnerKind.Evidence, item.Id, ct));
    }

    public async Task<EvidenceDto> CreateAsync(EvidenceWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var now = DateTimeOffset.UtcNow;
        var item = new EvidenceItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Title = request.Title.Trim(),
            Category = request.Category.Trim(),
            Notes = request.Notes?.Trim(),
            ExpiresOn = request.ExpiresOn,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.EvidenceItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.ToDto(now);
    }

    public async Task<EvidenceDto?> UpdateAsync(Guid id, EvidenceWriteRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        item.Title = request.Title.Trim();
        item.Category = request.Category.Trim();
        item.Notes = request.Notes?.Trim();
        item.ExpiresOn = request.ExpiresOn;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return item.ToDto(item.UpdatedAt, await _photos.CountAsync(PhotoOwnerKind.Evidence, item.Id, ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        await _photos.DeleteForOwnerAsync(PhotoOwnerKind.Evidence, item.Id, ct);
        if (!string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            await _files.DeleteAsync(item.TenantId, item.StoredFileName, ct);
        }

        _db.EvidenceItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EvidenceDto?> AttachFileAsync(Guid id, IFormFile file, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        if (!string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            await _files.DeleteAsync(item.TenantId, item.StoredFileName, ct);
        }

        await using var stream = file.OpenReadStream();
        var (stored, size) = await _files.SaveAsync(item.TenantId, item.Id, file.FileName, stream, ct);
        item.StoredFileName = stored;
        item.OriginalFileName = Path.GetFileName(file.FileName);
        item.ContentType = file.ContentType;
        item.FileSizeBytes = size;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return item.ToDto(item.UpdatedAt, await _photos.CountAsync(PhotoOwnerKind.Evidence, item.Id, ct));
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> OpenFileAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var item = await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (item is null || string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, item);
        var stream = await _files.OpenReadAsync(item.TenantId, item.StoredFileName, ct);
        if (stream is null)
        {
            return null;
        }

        return (stream, item.OriginalFileName ?? item.StoredFileName, item.ContentType ?? "application/octet-stream");
    }
}
