namespace SiteAnalyzer2.Serp;

public sealed class GoogleScrapePacing
{
    private readonly Lock _lock = new();
    private readonly List<DateTime> _requestTimestamps = [];
    private static readonly TimeSpan RollingWindow = TimeSpan.FromMinutes(30);

    public const string WarningMessage =
        "SERP pacing warning: recent requests clustered faster than human browsing rhythm; next delay lengthened proactively.";

    public int MinDelayMs =>
        int.TryParse(Environment.GetEnvironmentVariable("GOOGLE_SCRAPE_MIN_DELAY_MS"), out var value) && value > 0
            ? value
            : 5000;

    public async Task WaitBeforeRequestAsync(CancellationToken ct = default)
    {
        var delayMs = CalculateDelayMs();
        if (delayMs > 0)
            await Task.Delay(delayMs, ct);
    }

    public void RecordRequest()
    {
        lock (_lock)
        {
            _requestTimestamps.Add(DateTime.UtcNow);
            PruneOldTimestamps();
        }
    }

    public string? GetPacingWarning()
    {
        lock (_lock)
        {
            PruneOldTimestamps();
            if (_requestTimestamps.Count < 3)
                return null;

            var gaps = new List<double>();
            for (var i = 1; i < _requestTimestamps.Count; i++)
            {
                gaps.Add((_requestTimestamps[i] - _requestTimestamps[i - 1]).TotalMilliseconds);
            }

            if (gaps.Count == 0)
                return null;

            var averageGap = gaps.Average();
            return averageGap < MinDelayMs + 1500 ? WarningMessage : null;
        }
    }

    private int CalculateDelayMs()
    {
        var lengthen = GetPacingWarning() != null;
        var multiplier = lengthen ? 1.5 : 1.0;
        var jitter = Random.Shared.Next(0, 3001);
        var delay = (MinDelayMs + jitter) * multiplier;
        return (int)Math.Min(30_000, delay);
    }

    private void PruneOldTimestamps()
    {
        var cutoff = DateTime.UtcNow - RollingWindow;
        _requestTimestamps.RemoveAll(t => t < cutoff);
    }
}
