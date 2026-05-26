using GeekApplication.Interfaces.Seo;
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
        var result = await subscriptions.GetActiveTierAsync(user.RequireUserId(), ct);
        return result.IsSuccess ? Ok(new { tier = result.Value }) : BadRequest(result.Error);
    }
}
