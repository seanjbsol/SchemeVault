using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Auth;

/// <summary>
/// Request-scoped tenant identity. When <see cref="IsAvailable"/> is false
/// (startup, seeding, design-time, unauthenticated), EF global query filters are disabled.
/// </summary>
public interface ITenantProvider
{
    bool IsAvailable { get; }
    Guid TenantId { get; }
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid TenantId { get; }
    MembershipRole Role { get; }
    string? Email { get; }
}
