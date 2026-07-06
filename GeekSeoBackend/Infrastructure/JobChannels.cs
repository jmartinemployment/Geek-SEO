using System.Threading.Channels;

namespace GeekSeoBackend.Infrastructure;

public sealed class NicheAnalysisJobChannel(ILogger<NicheAnalysisJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("NicheAnalysisJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}
