using Microsoft.AspNetCore.Identity;

namespace SchemeVault.Api.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Home organisation created at signup. JWT <c>tenant_id</c> is this value for MVP.</summary>
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
