using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class TenantService
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public TenantService(AppDbContext db, ITenantProvider tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<TenantDto> GetCurrentAsync(CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
                     ?? throw new TenantAccessException();
        return new TenantDto { Id = tenant.Id, Name = tenant.Name };
    }

    public async Task<TenantDto> UpdateCurrentAsync(UpdateTenantRequest request, CancellationToken ct)
    {
        TenantGuard.EnsureAvailable(_tenant);
        if (_user.Role is not MembershipRole.Owner and not MembershipRole.Admin)
        {
            throw new UnauthorizedAccessException("Only an Owner or Admin can rename the organisation.");
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
                     ?? throw new TenantAccessException();
        tenant.Name = request.Name.Trim();
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new TenantDto { Id = tenant.Id, Name = tenant.Name };
    }
}
