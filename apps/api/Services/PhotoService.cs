using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class PhotoService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif", "image/heic", "image/heif"
    };

    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IFileStorage _files;

    public PhotoService(AppDbContext db, ITenantProvider tenant, IFileStorage files)
    {
        _db = db;
        _tenant = tenant;
        _files = files;
    }

    public async Task<IReadOnlyList<PhotoDto>> ListAsync(PhotoOwnerKind kind, Guid ownerId, CancellationToken ct)
    {
        await EnsureOwnerAsync(kind, ownerId, ct);
        var photos = await _db.Photos
            .Where(p => p.OwnerKind == kind && p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return photos.Select(ToDto).ToList();
    }

    public async Task<int> CountAsync(PhotoOwnerKind kind, Guid ownerId, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        return await _db.Photos.CountAsync(p => p.OwnerKind == kind && p.OwnerId == ownerId, ct);
    }

    public async Task<Dictionary<Guid, int>> CountByOwnerAsync(PhotoOwnerKind kind, IEnumerable<Guid> ownerIds, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var ids = ownerIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await _db.Photos
            .Where(p => p.OwnerKind == kind && ids.Contains(p.OwnerId))
            .GroupBy(p => p.OwnerId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    public async Task<PhotoDto?> GetAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, photo);
        return ToDto(photo);
    }

    public async Task<PhotoDto> AttachAsync(PhotoOwnerKind kind, Guid ownerId, IFormFile file, string? caption, CancellationToken ct)
    {
        await EnsureOwnerAsync(kind, ownerId, ct);
        if (file is null || file.Length == 0)
        {
            throw new InvalidOperationException("Choose a photo to upload.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? GuessContentType(file.FileName) : file.ContentType;
        if (!AllowedTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Upload a JPEG, PNG, WebP, GIF or HEIC photo.");
        }

        var photoId = Guid.NewGuid();
        await using var stream = file.OpenReadStream();
        var (stored, size) = await _files.SaveAsync(_tenant.TenantId, photoId, file.FileName, stream, ct);
        var photo = new PhotoAttachment
        {
            Id = photoId,
            TenantId = _tenant.TenantId,
            OwnerKind = kind,
            OwnerId = ownerId,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = stored,
            ContentType = contentType,
            FileSizeBytes = size,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync(ct);
        return ToDto(photo);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> OpenAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo is null)
        {
            return null;
        }

        TenantGuard.EnsureOwns(_tenant, photo);
        var stream = await _files.OpenReadAsync(photo.TenantId, photo.StoredFileName, ct);
        return stream is null ? null : (stream, photo.OriginalFileName, photo.ContentType);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (photo is null)
        {
            return false;
        }

        TenantGuard.EnsureOwns(_tenant, photo);
        await _files.DeleteAsync(photo.TenantId, photo.StoredFileName, ct);
        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteForOwnerAsync(PhotoOwnerKind kind, Guid ownerId, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var photos = await _db.Photos.Where(p => p.OwnerKind == kind && p.OwnerId == ownerId).ToListAsync(ct);
        foreach (var photo in photos)
        {
            TenantGuard.EnsureOwns(_tenant, photo);
            await _files.DeleteAsync(photo.TenantId, photo.StoredFileName, ct);
        }

        _db.Photos.RemoveRange(photos);
    }

    private async Task EnsureOwnerAsync(PhotoOwnerKind kind, Guid ownerId, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        ITenantOwned? owner = kind switch
        {
            PhotoOwnerKind.Evidence => await _db.EvidenceItems.FirstOrDefaultAsync(e => e.Id == ownerId, ct),
            PhotoOwnerKind.Accident => await _db.Accidents.FirstOrDefaultAsync(e => e.Id == ownerId, ct),
            PhotoOwnerKind.Equipment => await _db.Equipment.FirstOrDefaultAsync(e => e.Id == ownerId, ct),
            _ => null
        };

        if (owner is null)
        {
            throw new TenantAccessException();
        }

        TenantGuard.EnsureOwns(_tenant, owner);
    }

    private static PhotoDto ToDto(PhotoAttachment photo) => new()
    {
        Id = photo.Id,
        OwnerKind = photo.OwnerKind.ToString(),
        OwnerId = photo.OwnerId,
        Caption = photo.Caption,
        OriginalFileName = photo.OriginalFileName,
        ContentType = photo.ContentType,
        FileSizeBytes = photo.FileSizeBytes,
        CreatedAt = photo.CreatedAt
    };

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
}
