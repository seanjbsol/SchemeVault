namespace SchemeVault.Api.Billing;

public sealed class SubscriptionApiOptions
{
    public const string SectionName = "SubscriptionApi";

    /// <summary>Origin of the central QckApp Subscription API, e.g. https://subscription.qckapp.example.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Server API key sent as <c>X-Api-Key</c>. Do not commit production values.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string ProductCode { get; set; } = "SchemeVault";

    /// <summary>
    /// When true, no HTTP calls are made. Every tenant is treated as active on
    /// <see cref="StubPlan"/> so tests and local runs work without a live Qck API.
    /// </summary>
    public bool UseStub { get; set; }

    public string StubStatus { get; set; } = "active";

    public string StubPlan { get; set; } = "Starter";

    /// <summary>
    /// When true (typical in Development stub mode), the tenant is treated as
    /// SchemeVault Pro so questionnaires, accidents and equipment can be demoed
    /// without a live Qck plan. Does not bypass an inactive subscription.
    /// </summary>
    public bool ForcePro { get; set; }

    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
}
