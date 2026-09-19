using System.Text.Json.Serialization;

namespace SchemeVault.Api.Billing;

public sealed class EntitlementsDto
{
    public string Status { get; set; } = "inactive";
    public string? Plan { get; set; }
    public string? PlanCode { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }

    [JsonIgnore]
    public bool IsActiveOrTrialing =>
        IsActive ||
        StatusEquals("active") ||
        StatusEquals("trialing") ||
        StatusEquals("trial");

    public static EntitlementsDto Create(string status, string? plan, string? planCode = null)
    {
        var dto = new EntitlementsDto
        {
            Status = string.IsNullOrWhiteSpace(status) ? "inactive" : status,
            Plan = plan,
            PlanCode = planCode
        };
        dto.IsActive = dto.IsActiveOrTrialing;
        return dto;
    }

    private bool StatusEquals(string expected) =>
        string.Equals(Status, expected, StringComparison.OrdinalIgnoreCase);
}

public sealed class BillingSessionRequest
{
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class BillingSessionResponse
{
    public string Url { get; set; } = string.Empty;
}

public sealed class PaymentRequiredBody
{
    public string Title { get; set; } = "An active SchemeVault subscription is required.";
    public int Status { get; set; } = StatusCodes.Status402PaymentRequired;
    public string SubscriptionStatus { get; set; } = "inactive";
    public string? Plan { get; set; }
    public string Checkout { get; set; } = "/api/billing/checkout";
    public string Detail { get; set; } =
        "POST /api/billing/checkout (Owner/Admin) to start or resume billing. Do not call Stripe from SchemeVault.";
}

public sealed class QckUpsertTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string ExternalTenantId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = "SchemeVault";
}

public sealed class QckCheckoutSessionRequest
{
    public string ProductCode { get; set; } = "SchemeVault";
    public string ExternalTenantId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string? TenantName { get; set; }
}

public sealed class QckPortalSessionRequest
{
    public string ProductCode { get; set; } = "SchemeVault";
    public string ExternalTenantId { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
