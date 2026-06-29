using System.Threading.Channels;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

/// <summary>
/// In-process producer/consumer queue for competitor crawl progress.
/// Crawl workers publish; a hosted relay pushes to SignalR without blocking HTTP threads.
/// </summary>
public sealed class CompetitorCrawlProgressPublisher
{
    private readonly Channel<CompetitorCrawlProgressEvent> _channel =
        Channel.CreateUnbounded<CompetitorCrawlProgressEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false,
        });

    public ChannelReader<CompetitorCrawlProgressEvent> Reader => _channel.Reader;

    public void Publish(CompetitorCrawlProgressEvent progress) =>
        _channel.Writer.TryWrite(progress);
}
