namespace SchemeVault.Api.Billing;

public interface ISubscriptionClient
{
    Task<EntitlementsDto> GetEntitlementsAsync(Guid tenantId, CancellationToken ct);

    Task UpsertTenantAsync(QckUpsertTenantRequest request, CancellationToken ct);

    Task<BillingSessionResponse> CreateCheckoutSessionAsync(QckCheckoutSessionRequest request, CancellationToken ct);

    Task<BillingSessionResponse> CreatePortalSessionAsync(QckPortalSessionRequest request, CancellationToken ct);
}
