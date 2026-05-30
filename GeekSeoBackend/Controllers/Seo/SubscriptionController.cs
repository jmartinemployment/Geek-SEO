using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/subscription")]
public sealed class SubscriptionController(ISubscriptionService subscriptions, ICurrentUserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var result = await subscriptions.GetActiveTierAsync(user.RequireUserId(), ct);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(new { tier = result.Value!.ToString().ToLowerInvariant() });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required" });
        }
    }
}
