using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SiteAnalyzer2.Api.Realtime;

/// <summary>
/// Routes Postgres NOTIFY payloads to active SSE streams on this API replica.
/// </summary>
public sealed class CrawlProgressBroadcaster
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<string>>> _streams = new();

    public (ChannelReader<string> Reader, Guid SubscriptionId) Subscribe(string runId)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false,
        });

        var runStreams = _streams.GetOrAdd(runId, _ => new ConcurrentDictionary<Guid, Channel<string>>());
        runStreams[subscriptionId] = channel;
        return (channel.Reader, subscriptionId);
    }

    public void Unsubscribe(string runId, Guid subscriptionId)
    {
        if (!_streams.TryGetValue(runId, out var runStreams))
            return;

        if (runStreams.TryRemove(subscriptionId, out var channel))
            channel.Writer.TryComplete();

        if (runStreams.IsEmpty)
            _streams.TryRemove(runId, out _);
    }

    public void BroadcastToRun(string runId, string jsonPayload)
    {
        if (!_streams.TryGetValue(runId, out var runStreams))
            return;

        foreach (var (_, channel) in runStreams)
            channel.Writer.TryWrite(jsonPayload);
    }
}
