using Microsoft.Extensions.Options;

namespace SchemeVault.Api.Billing;

/// <summary>
/// Local/test stand-in for the QckApp Subscription API. Does not call the network.
/// Default: every tenant is active on Starter.
/// </summary>
public sealed class StubSubscriptionClient : ISubscriptionClient
{
    public const string CheckoutUrl = "https://billing.qckapp.test/checkout/schemevault";
    public const string PortalUrl = "https://billing.qckapp.test/portal/schemevault";

    private readonly SubscriptionApiOptions _options;
    private readonly ILogger<StubSubscriptionClient> _logger;

    public StubSubscriptionClient(IOptions<SubscriptionApiOptions> options, ILogger<StubSubscriptionClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<EntitlementsDto> GetEntitlementsAsync(Guid tenantId, CancellationToken ct)
    {
        var dto = EntitlementsDto.Create(_options.StubStatus, _options.StubPlan, "starter");
        return Task.FromResult(dto);
    }

    public Task UpsertTenantAsync(QckUpsertTenantRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Stub Qck upsert tenant {ExternalTenantId} ({Name}, {Email})",
            request.ExternalTenantId,
            request.Name,
            request.OwnerEmail);
        return Task.CompletedTask;
    }

    public Task<BillingSessionResponse> CreateCheckoutSessionAsync(QckCheckoutSessionRequest request, CancellationToken ct) =>
        Task.FromResult(new BillingSessionResponse { Url = CheckoutUrl });

    public Task<BillingSessionResponse> CreatePortalSessionAsync(QckPortalSessionRequest request, CancellationToken ct) =>
        Task.FromResult(new BillingSessionResponse { Url = PortalUrl });
}
