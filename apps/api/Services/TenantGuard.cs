using SchemeVault.Api.Auth;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class TenantAccessException : Exception
{
    public TenantAccessException() : base("The requested resource does not belong to this organisation.")
    {
    }
}

public static class TenantGuard
{
    /// <summary>
    /// Belt-and-braces check in addition to EF global query filters.
    /// Throws rather than returning another tenant's data.
    /// </summary>
    public static void EnsureOwns(ITenantProvider tenant, ITenantOwned entity)
    {
        if (!tenant.IsAvailable || entity.TenantId != tenant.TenantId)
        {
            throw new TenantAccessException();
        }
    }

    public static void EnsureAvailable(ITenantProvider tenant)
    {
        if (!tenant.IsAvailable)
        {
            throw new TenantAccessException();
        }
    }
}
