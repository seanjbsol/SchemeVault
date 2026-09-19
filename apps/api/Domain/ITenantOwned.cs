namespace SchemeVault.Api.Domain;

/// <summary>
/// Operational (tenant-scoped) entities implement this. Global catalogues such as
/// <see cref="Scheme"/> and <see cref="GapChecklistTemplate"/> do not.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
