using System.Net.Http.Headers;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Services;

/// <summary>
/// Forwards user identity to GeekAPI internal routes (Bearer and/or X-Geek-User-Id) plus service API key.
/// </summary>
public sealed class GeekDataGatewayHandler(
    IHttpContextAccessor httpContextAccessor,
    WorkerUserContext workerUser) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (HttpContextAccess.TryGetLiveContext(httpContextAccessor) is { } ctx)
        {
            var header = ctx.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(header)
                && AuthenticationHeaderValue.TryParse(header, out var parsed))
            {
                request.Headers.Authorization = parsed;
            }

            if (workerUser.UserId == Guid.Empty)
            {
                var sub = ctx.User.FindFirst("sub")?.Value
                    ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(sub, out var userId) && userId != Guid.Empty)
                    request.Headers.TryAddWithoutValidation("X-Geek-User-Id", userId.ToString());
                else if (ctx.Request.Headers.TryGetValue("X-User-Id", out var devUser)
                    && Guid.TryParse(devUser, out userId))
                    request.Headers.TryAddWithoutValidation("X-Geek-User-Id", userId.ToString());
            }
        }

        if (workerUser.UserId != Guid.Empty)
            request.Headers.TryAddWithoutValidation("X-Geek-User-Id", workerUser.UserId.ToString());

        var apiKey = Environment.GetEnvironmentVariable("GEEK_BACKEND_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
