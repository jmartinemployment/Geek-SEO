namespace GeekSeoBackend.Providers.Seo.SerperDev;

/// <summary>Process-wide throttle for Serper.dev (~5 req/s plan). Serializes spacing between HTTP calls.</summary>
internal static class SerperRateGate
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static DateTimeOffset _lastRequestUtc = DateTimeOffset.MinValue;

    /// <summary>Minimum gap between Serper HTTP calls. Override via SERPER_MIN_INTERVAL_MS.</summary>
    internal static int MinIntervalMs =>
        int.TryParse(Environment.GetEnvironmentVariable("SERPER_MIN_INTERVAL_MS"), out var ms) && ms > 0
            ? ms
            : 250;

    internal static async Task WaitAsync(CancellationToken ct)
    {
        await Lock.WaitAsync(ct);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestUtc;
            var waitMs = MinIntervalMs - (int)elapsed.TotalMilliseconds;
            if (waitMs > 0)
                await Task.Delay(waitMs, ct);
            _lastRequestUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            Lock.Release();
        }
    }
}
