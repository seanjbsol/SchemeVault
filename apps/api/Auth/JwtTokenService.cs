using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "SchemeVault";
    public string Audience { get; set; } = "SchemeVault.Mobile";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public (string Token, DateTimeOffset ExpiresAt) Create(ApplicationUser user, MembershipRole role, string tenantName)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be set to at least 32 characters. Use the Jwt__SigningKey environment variable in production.");
        }

        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(HttpRequestContext.TenantIdClaim, user.TenantId.ToString()),
            new(HttpRequestContext.RoleClaim, role.ToString()),
            new(ClaimTypes.Role, role.ToString()),
            new("full_name", user.FullName),
            new("tenant_name", tenantName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        _logger.LogDebug("Issued JWT for user {UserId} tenant {TenantId} role {Role}", user.Id, user.TenantId, role);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
