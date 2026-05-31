using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/dashboard")]
public sealed class DashboardController(
    DashboardOverviewService dashboard,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var overview = await dashboard.GetOverviewAsync(user.RequireUserId(), ct);
        return Ok(overview);
    }
}
