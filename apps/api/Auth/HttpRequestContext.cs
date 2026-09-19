using System.Security.Claims;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Auth;

public sealed class HttpRequestContext : ITenantProvider, ICurrentUser
{
    public const string TenantIdClaim = "tenant_id";
    public const string RoleClaim = "role";

    private readonly IHttpContextAccessor _http;

    public HttpRequestContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsAvailable => IsAuthenticated && TenantId != Guid.Empty;

    public Guid TenantId => ParseGuid(TenantIdClaim);

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public MembershipRole Role
    {
        get
        {
            var raw = User?.FindFirstValue(RoleClaim) ?? User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<MembershipRole>(raw, ignoreCase: true, out var role)
                ? role
                : MembershipRole.Member;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");

    private Guid ParseGuid(string claimType)
    {
        var raw = User?.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}

public sealed class NullTenantProvider : ITenantProvider
{
    public bool IsAvailable => false;
    public Guid TenantId => Guid.Empty;
}
