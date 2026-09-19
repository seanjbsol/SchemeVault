namespace SchemeVault.Api.Domain;

public class PhotoAttachment : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public PhotoOwnerKind OwnerKind { get; set; }
    public Guid OwnerId { get; set; }
    public string? Caption { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
