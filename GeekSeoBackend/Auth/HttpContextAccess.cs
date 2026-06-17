namespace GeekSeoBackend.Auth;

/// <summary>
/// Safe access to <see cref="IHttpContextAccessor.HttpContext"/> after fire-and-forget
/// step jobs outlive the originating HTTP request.
/// </summary>
internal static class HttpContextAccess
{
    internal static HttpContext? TryGetLiveContext(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        if (ctx is null)
            return null;

        try
        {
            _ = ctx.Features;
            return ctx;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
    }
}
