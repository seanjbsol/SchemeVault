namespace SchemeVault.Api.Domain;

public class Accreditation : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid SchemeId { get; set; }
    public Scheme Scheme { get; set; } = null!;
    public AccreditationStatus Status { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public DateTimeOffset? LastSubmittedOn { get; set; }
    public string? MembershipNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
