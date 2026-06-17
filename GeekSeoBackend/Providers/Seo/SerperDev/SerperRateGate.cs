namespace GeekSeoBackend.Providers.Seo.SerperDev;

/// <summary>
/// Process-wide throttle for Serper.dev (~5 req/s plan). Serializes HTTP calls and backs off on 429.
/// </summary>
internal static class SerperRateGate
{
    private static readonly SemaphoreSlim Serial = new(1, 1);
    private static DateTimeOffset _lastCompletedUtc = DateTimeOffset.MinValue;
    private static DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;

    /// <summary>Minimum gap between completed Serper HTTP calls. Override via SERPER_MIN_INTERVAL_MS.</summary>
    internal static int MinIntervalMs =>
        int.TryParse(Environment.GetEnvironmentVariable("SERPER_MIN_INTERVAL_MS"), out var ms) && ms > 0
            ? ms
            : 350;

    /// <summary>Global pause after HTTP 429. Override via SERPER_COOLDOWN_SECONDS.</summary>
    internal static int CooldownSecondsOn429 =>
        int.TryParse(Environment.GetEnvironmentVariable("SERPER_COOLDOWN_SECONDS"), out var seconds) && seconds > 0
            ? seconds
            : 5;

    internal static void PenalizeForRateLimit()
    {
        var until = DateTimeOffset.UtcNow.AddSeconds(CooldownSecondsOn429);
        if (until > _cooldownUntil)
            _cooldownUntil = until;
    }

    internal static async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await Serial.WaitAsync(ct);
        try
        {
            await WaitForSlotAsync(ct);
            var result = await action();
            _lastCompletedUtc = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            Serial.Release();
        }
    }

    private static async Task WaitForSlotAsync(CancellationToken ct)
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            var earliest = _lastCompletedUtc.AddMilliseconds(MinIntervalMs);
            if (_cooldownUntil > earliest)
                earliest = _cooldownUntil;

            var delay = earliest - now;
            if (delay <= TimeSpan.Zero)
                return;

            await Task.Delay(delay, ct);
        }
    }
}
