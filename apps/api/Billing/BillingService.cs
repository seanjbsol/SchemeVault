using Microsoft.Extensions.Options;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Billing;

public sealed class BillingService
{
    private readonly ISubscriptionClient _client;
    private readonly ICurrentUser _user;
    private readonly AppDbContext _db;
    private readonly SubscriptionApiOptions _options;

    public BillingService(
        ISubscriptionClient client,
        ICurrentUser user,
        AppDbContext db,
        IOptions<SubscriptionApiOptions> options)
    {
        _client = client;
        _user = user;
        _db = db;
        _options = options.Value;
    }

    public Task<EntitlementsDto> GetEntitlementsAsync(CancellationToken ct)
    {
        EnsureSignedIn();
        return _client.GetEntitlementsAsync(_user.TenantId, ct);
    }

    public async Task<BillingSessionResponse> CreateCheckoutAsync(BillingSessionRequest request, CancellationToken ct)
    {
        EnsureOwnerOrAdmin();
        var tenant = await LoadTenantAsync(ct);
        var success = FirstNonEmpty(request.SuccessUrl, request.ReturnUrl) ?? "https://schemevault.app/billing/success";
        var cancel = FirstNonEmpty(request.CancelUrl) ?? "https://schemevault.app/billing/cancel";
        return await _client.CreateCheckoutSessionAsync(new QckCheckoutSessionRequest
        {
            ProductCode = _options.ProductCode,
            ExternalTenantId = tenant.Id.ToString("D"),
            SuccessUrl = success,
            CancelUrl = cancel,
            OwnerEmail = _user.Email,
            TenantName = tenant.Name
        }, ct);
    }

    public async Task<BillingSessionResponse> CreatePortalAsync(BillingSessionRequest request, CancellationToken ct)
    {
        EnsureOwnerOrAdmin();
        var tenant = await LoadTenantAsync(ct);
        return await _client.CreatePortalSessionAsync(new QckPortalSessionRequest
        {
            ProductCode = _options.ProductCode,
            ExternalTenantId = tenant.Id.ToString("D"),
            ReturnUrl = FirstNonEmpty(request.ReturnUrl, request.SuccessUrl)
        }, ct);
    }

    private void EnsureSignedIn()
    {
        if (!_user.IsAuthenticated || _user.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Not signed in.");
        }
    }

    private void EnsureOwnerOrAdmin()
    {
        EnsureSignedIn();
        if (_user.Role is not MembershipRole.Owner and not MembershipRole.Admin)
        {
            throw new UnauthorizedAccessException("Only an Owner or Admin can manage billing.");
        }
    }

    private async Task<Tenant> LoadTenantAsync(CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([_user.TenantId], ct)
                     ?? throw new TenantAccessException();
        return tenant;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
