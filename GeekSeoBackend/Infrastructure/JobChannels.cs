using System.Threading.Channels;

namespace GeekSeoBackend.Infrastructure;

/// <summary>In-memory Through Coverage job — no site_analysis_profiles row until persist.</summary>
public sealed record ThroughCoverageJob(Guid UserId, Guid ProjectId, string Domain, string? SeedTopic);

public sealed class SiteAnalysisJobChannel(ILogger<SiteAnalysisJobChannel> logger)
{
    private readonly Channel<ThroughCoverageJob> _channel = Channel.CreateBounded<ThroughCoverageJob>(
        new BoundedChannelOptions(50) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enqueue(ThroughCoverageJob job)
    {
        if (!_channel.Writer.TryWrite(job))
            logger.LogWarning("Through coverage job dropped — channel full");
    }

    public ChannelReader<ThroughCoverageJob> Reader => _channel.Reader;
}
