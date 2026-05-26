using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/serp-cache")]
public sealed class SerpCacheController(IHttpClientFactory factory, ICurrentUserContext user) : ControllerBase
{
    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] string keyword,
        [FromQuery] string location,
        [FromQuery] string languageCode = "en",
        CancellationToken ct = default)
    {
        _ = user.RequireUserId();
        var http = factory.CreateClient(GeekDataGateway.HttpClientName);
        var url =
            $"api/seo/internal/serp-cache?keyword={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&languageCode={languageCode}";
        var response = await http.DeleteAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return BadRequest(await response.Content.ReadAsStringAsync(ct));
        return NoContent();
    }
}
