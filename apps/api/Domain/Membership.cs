namespace SchemeVault.Api.Domain;

public class Membership : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public MembershipRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
