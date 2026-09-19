using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchemeVault.Api.Auth;

namespace SchemeVault.Api.Billing;

/// <summary>Skip the Qck subscription gate (auth, billing, and similar).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipSubscriptionAttribute : Attribute, IFilterMetadata;

/// <summary>
/// After authentication, require an active or trialing SchemeVault subscription
/// for tenant resource endpoints. Inactive tenants receive 402 with a checkout pointer.
/// </summary>
public sealed class SubscriptionGateFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<AllowAnonymousAttribute>().Any() ||
            metadata.OfType<SkipSubscriptionAttribute>().Any())
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

        var client = context.HttpContext.RequestServices.GetRequiredService<ISubscriptionClient>();
        var options = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<SubscriptionApiOptions>>().Value;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SubscriptionGateFilter>>();

        EntitlementsDto entitlements;
        try
        {
            entitlements = EntitlementsNormalizer.Apply(
                await client.GetEntitlementsAsync(current.TenantId, context.HttpContext.RequestAborted),
                options);
            context.HttpContext.Items[EntitlementsNormalizer.HttpItemKey] = entitlements;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogError(ex, "Failed to read Qck entitlements for tenant {TenantId}", current.TenantId);
            context.Result = new ObjectResult(new { title = "Billing service unavailable." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        if (entitlements.IsActiveOrTrialing)
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new PaymentRequiredBody
        {
            SubscriptionStatus = entitlements.Status,
            Plan = entitlements.Plan
        })
        {
            StatusCode = StatusCodes.Status402PaymentRequired
        };
    }
}
