using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace GeekSeoBackend.Middleware;

public sealed class PublicRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private const int MaxRequestsPerHour = 3;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/public/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress;
        if (ip is null || IPAddress.IsLoopback(ip))
        {
            await next(context);
            return;
        }

        var key = $"public-rate:{ip}";
        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return 0;
        });

        if (count >= MaxRequestsPerHour)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many scans from this network. Try again in an hour or sign up for a free account.",
            });
            return;
        }

        cache.Set(key, count + 1, Window);
        await next(context);
    }
}
