using Microsoft.AspNetCore.Http.Features;
using SiteAnalyzer2.Api.Realtime;

namespace SiteAnalyzer2.Api.Endpoints;

public static class CompetitorCrawlProgressStreamEndpoint
{
    private static readonly TimeSpan KeepaliveInterval = TimeSpan.FromSeconds(15);

    public static void MapCompetitorCrawlProgressStream(this WebApplication app)
    {
        app.MapGet("/runs/{runId:guid}/competitor-crawl/progress-stream", (
            Guid runId,
            HttpContext context,
            CrawlProgressBroadcaster broadcaster,
            CancellationToken ct) =>
            StreamCompetitorCrawlProgressAsync(runId, context, broadcaster, ct))
            .RequireCors();
    }

    private static async Task StreamCompetitorCrawlProgressAsync(
        Guid runId,
        HttpContext context,
        CrawlProgressBroadcaster broadcaster,
        CancellationToken ct)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache, no-transform";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        var bufferingFeature = context.Features.Get<IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        var runKey = runId.ToString();
        var (reader, subscriptionId) = broadcaster.Subscribe(runKey);

        // Flush headers immediately so browsers/proxies see CORS + 200 before the first crawl event.
        await context.Response.WriteAsync(": connected\n\n", ct);
        await context.Response.Body.FlushAsync(ct);

        using var keepaliveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var keepaliveTask = SendKeepalivesAsync(context, keepaliveCts.Token);

        try
        {
            await foreach (var jsonPayload in reader.ReadAllAsync(ct).WithCancellation(ct))
            {
                await context.Response.WriteAsync($"data: {jsonPayload}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
        }
        finally
        {
            keepaliveCts.Cancel();
            try
            {
                await keepaliveTask;
            }
            catch (OperationCanceledException)
            {
                // expected when the client disconnects
            }

            broadcaster.Unsubscribe(runKey, subscriptionId);
        }
    }

    private static async Task SendKeepalivesAsync(HttpContext context, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(KeepaliveInterval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await context.Response.WriteAsync(": keepalive\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }
}
