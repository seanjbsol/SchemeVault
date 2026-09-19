namespace SchemeVault.Api.Domain;

public class GapChecklistItem : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid SchemeId { get; set; }
    public Scheme Scheme { get; set; } = null!;
    public Guid? TemplateId { get; set; }
    public GapChecklistTemplate? Template { get; set; }
    public Guid? EvidenceItemId { get; set; }
    public EvidenceItem? EvidenceItem { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GapItemStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
