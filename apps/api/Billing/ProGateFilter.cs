using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SchemeVault.Api.Auth;

namespace SchemeVault.Api.Billing;

/// <summary>Mark an endpoint as SchemeVault Pro. Combined with <see cref="ProGateFilter"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireProAttribute : Attribute, IFilterMetadata
{
    public RequireProAttribute(string? feature = null)
    {
        Feature = feature;
    }

    public string? Feature { get; }
}

/// <summary>
/// After the subscription gate, require Pro (plan code, feature flags, or ForcePro)
/// for endpoints marked <see cref="RequireProAttribute"/>. Starter tenants get 402.
/// </summary>
public sealed class ProGateFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var required = context.ActionDescriptor.EndpointMetadata.OfType<RequireProAttribute>().LastOrDefault();
        if (required is null)
        {
            await next();
            return;
        }

        var current = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!current.IsAuthenticated || current.TenantId == Guid.Empty)
        {
            await next();
            return;
        }

        var entitlements = await ResolveEntitlementsAsync(context, current.TenantId);
        if (entitlements is null)
        {
            return;
        }

        if (EntitlementsNormalizer.HasFeature(entitlements, required.Feature))
        {
            await next();
            return;
        }

        var featureLabel = string.IsNullOrWhiteSpace(required.Feature)
            ? "this feature"
            : required.Feature.Replace('_', ' ');

        context.Result = new ObjectResult(new PaymentRequiredBody
        {
            Title = $"SchemeVault Pro is required for {featureLabel}.",
            SubscriptionStatus = entitlements.Status,
            Plan = entitlements.Plan,
            RequiredPlan = "Pro",
            Feature = required.Feature,
            Detail =
                "Upgrade to SchemeVault Pro for guided questionnaires, multi-scheme pack export, accident reporting and the equipment register. POST /api/billing/checkout (Owner/Admin). Do not call Stripe from SchemeVault."
        })
        {
            StatusCode = StatusCodes.Status402PaymentRequired
        };
    }

    private static async Task<EntitlementsDto?> ResolveEntitlementsAsync(ActionExecutingContext context, Guid tenantId)
    {
        if (context.HttpContext.Items.TryGetValue(EntitlementsNormalizer.HttpItemKey, out var cached) &&
            cached is EntitlementsDto dto)
        {
            return dto;
        }

        var client = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionClient>();
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<SubscriptionApiOptions>>().Value;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ProGateFilter>>();
        try
        {
            var fresh = EntitlementsNormalizer.Apply(
                await client.GetEntitlementsAsync(tenantId, context.HttpContext.RequestAborted),
                options);
            context.HttpContext.Items[EntitlementsNormalizer.HttpItemKey] = fresh;
            return fresh;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to read Qck entitlements for tenant {TenantId}", tenantId);
            context.Result = new ObjectResult(new { title = "Billing service unavailable." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return null;
        }
    }
}
