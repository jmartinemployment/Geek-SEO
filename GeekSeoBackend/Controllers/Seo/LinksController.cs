using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/links")]
public sealed class LinksController(IInternalLinkService links, ICurrentUserContext user) : ControllerBase
{
    [HttpPost("suggest")]
    public async Task<IActionResult> Suggest([FromBody] InternalLinkSuggestRequest request, CancellationToken ct)
    {
        var result = await links.SuggestAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("auto-insert")]
    public async Task<IActionResult> AutoInsert([FromBody] InternalLinkAutoInsertRequest request, CancellationToken ct)
    {
        var result = await links.AutoInsertAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
