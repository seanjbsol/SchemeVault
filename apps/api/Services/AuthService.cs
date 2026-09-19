using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Services;

public sealed class AuthService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _jwt;
    private readonly ICurrentUser _current;

    public AuthService(AppDbContext db, UserManager<ApplicationUser> users, JwtTokenService jwt, ICurrentUser current)
    {
        _db = db;
        _users = users;
        _jwt = jwt;
        _current = current;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.OrganisationName.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            CreatedAt = now
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        _db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = MembershipRole.Owner,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);
        await SeedData.ProvisionTenantGapsAsync(_db, tenant.Id, now);

        var (token, expires) = _jwt.Create(user, MembershipRole.Owner, tenant.Name);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expires,
            User = user.ToDto(tenant.Name, MembershipRole.Owner)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(email);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Email or password is incorrect.");
        }

        var membership = await _db.Memberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.TenantId == user.TenantId, ct)
            ?? throw new UnauthorizedAccessException("This account has no organisation membership.");

        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == user.TenantId, ct);
        var (token, expires) = _jwt.Create(user, membership.Role, tenant.Name);
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = expires,
            User = user.ToDto(tenant.Name, membership.Role)
        };
    }

    public async Task<UserDto> MeAsync(CancellationToken ct)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == _current.UserId, ct)
                   ?? throw new UnauthorizedAccessException("Not signed in.");
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == user.TenantId, ct);
        return user.ToDto(tenant.Name, _current.Role);
    }
}
