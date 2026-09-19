using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SchemeVault.Api.Billing;

[ApiController]
[Authorize]
[SkipSubscription]
[Route("api/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly BillingService _billing;

    public BillingController(BillingService billing)
    {
        _billing = billing;
    }

    [HttpGet("entitlements")]
    public async Task<ActionResult<EntitlementsDto>> Entitlements(CancellationToken ct) =>
        Ok(await _billing.GetEntitlementsAsync(ct));

    [HttpPost("checkout")]
    public async Task<ActionResult<BillingSessionResponse>> Checkout(
        [FromBody] BillingSessionRequest? request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _billing.CreateCheckoutAsync(request ?? new BillingSessionRequest(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { title = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { title = ex.Message });
        }
    }

    [HttpPost("portal")]
    public async Task<ActionResult<BillingSessionResponse>> Portal(
        [FromBody] BillingSessionRequest? request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _billing.CreatePortalAsync(request ?? new BillingSessionRequest(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { title = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { title = ex.Message });
        }
    }
}
