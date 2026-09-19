using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SchemeVault.Api.Billing;

public sealed class SubscriptionClient : ISubscriptionClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SubscriptionApiOptions _options;
    private readonly ILogger<SubscriptionClient> _logger;

    public SubscriptionClient(HttpClient http, IOptions<SubscriptionApiOptions> options, ILogger<SubscriptionClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EntitlementsDto> GetEntitlementsAsync(Guid tenantId, CancellationToken ct)
    {
        var product = Uri.EscapeDataString(_options.ProductCode);
        using var response = await SendAsync(
            HttpMethod.Get,
            $"api/v1/entitlements/{product}/{tenantId:D}",
            content: null,
            ct);
        await EnsureSuccessAsync(response, "entitlements", ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ParseEntitlements(doc.RootElement);
    }

    public async Task UpsertTenantAsync(QckUpsertTenantRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/v1/tenants", request, ct);
        await EnsureSuccessAsync(response, "tenant upsert", ct);
    }

    public async Task<BillingSessionResponse> CreateCheckoutSessionAsync(QckCheckoutSessionRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/v1/checkout/sessions", request, ct);
        await EnsureSuccessAsync(response, "checkout session", ct);
        return await ReadSessionUrlAsync(response, ct, "checkoutUrl", "sessionUrl", "url");
    }

    public async Task<BillingSessionResponse> CreatePortalSessionAsync(QckPortalSessionRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/v1/portal/sessions", request, ct);
        await EnsureSuccessAsync(response, "portal session", ct);
        return await ReadSessionUrlAsync(response, ct, "customerPortalUrl", "portalUrl", "url");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativeUrl, object? content, CancellationToken ct)
    {
        EnsureConfigured();
        using var request = new HttpRequestMessage(method, relativeUrl);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
        }

        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: Json);
        }

        return await _http.SendAsync(request, ct);
    }

    private void EnsureConfigured()
    {
        if (_http.BaseAddress is null)
        {
            throw new InvalidOperationException(
                "SubscriptionApi:BaseUrl is not set. Point it at the QckApp Subscription API or enable SubscriptionApi:UseStub.");
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning(
            "Qck Subscription API {Operation} failed with {Status}: {Body}",
            operation,
            (int)response.StatusCode,
            Truncate(body));
        throw new HttpRequestException(
            $"Qck Subscription API {operation} failed with {(int)response.StatusCode}.");
    }

    private async Task<BillingSessionResponse> ReadSessionUrlAsync(
        HttpResponseMessage response,
        CancellationToken ct,
        params string[] urlPropertyNames)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var url = ReadString(doc.RootElement, urlPropertyNames)
                  ?? ReadString(doc.RootElement, "customerPortalUrl", "portalUrl");
        if (doc.RootElement.TryGetProperty("token", out var token) &&
            token.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(url))
        {
            url = AppendToken(url, token.GetString());
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Qck Subscription API did not return a session URL.");
        }

        return new BillingSessionResponse { Url = url };
    }

    private static string? AppendToken(string url, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return url;
        }

        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}token={Uri.EscapeDataString(token)}";
    }

    internal static EntitlementsDto ParseEntitlements(JsonElement root)
    {
        var source = root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("entitlement", out var nested) && nested.ValueKind == JsonValueKind.Object)
            {
                source = nested;
            }
            else if (root.TryGetProperty("entitlements", out var list) &&
                     list.ValueKind == JsonValueKind.Array &&
                     list.GetArrayLength() > 0)
            {
                source = list[0];
            }
        }

        var status = ReadString(source, "status", "subscriptionStatus", "state") ?? "inactive";
        var plan = ReadString(source, "plan", "planName", "planCode", "productName");
        var planCode = ReadString(source, "planCode", "planIdentifier", "priceId");
        var dto = EntitlementsDto.Create(status, plan, planCode);
        dto.TrialEndsAt = ReadDate(source, "trialEndsAt", "trialEnd");
        dto.CurrentPeriodEnd = ReadDate(source, "currentPeriodEnd", "periodEnd", "expiresAt");
        return dto;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static DateTimeOffset? ReadDate(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var prop))
            {
                continue;
            }

            if (prop.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(prop.GetString(), out var parsed))
            {
                return parsed;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var unix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unix);
            }
        }

        return null;
    }

    private static string Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= 500 ? value : value[..500];
}
