using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/brand-voices")]
public sealed class BrandVoicesController(IBrandVoiceService voices, ICurrentUserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await voices.ListAsync(user.RequireUserId(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await voices.GetAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrandVoiceRequest request, CancellationToken ct)
    {
        var result = await voices.CreateAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandVoiceRequest request, CancellationToken ct)
    {
        var result = await voices.UpdateAsync(user.RequireUserId(), id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await voices.DeleteAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
