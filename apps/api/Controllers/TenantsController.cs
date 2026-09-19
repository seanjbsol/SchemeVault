using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchemeVault.Api.Contracts;
using SchemeVault.Api.Services;

namespace SchemeVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly TenantService _tenants;

    public TenantsController(TenantService tenants)
    {
        _tenants = tenants;
    }

    [HttpGet("current")]
    public async Task<ActionResult<TenantDto>> Current(CancellationToken ct) =>
        Ok(await _tenants.GetCurrentAsync(ct));

    [HttpPatch("current")]
    public async Task<ActionResult<TenantDto>> Update([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _tenants.UpdateCurrentAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { title = ex.Message });
        }
    }
}
