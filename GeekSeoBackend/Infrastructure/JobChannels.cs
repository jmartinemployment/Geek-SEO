using System.Threading.Channels;

namespace GeekSeoBackend.Infrastructure;

public sealed class FullArticleJobChannel(ILogger<FullArticleJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("FullArticleJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}

public sealed class BulkArticleJobChannel(ILogger<BulkArticleJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("BulkArticleJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}

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

public sealed class UrlResearchJobChannel(ILogger<UrlResearchJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("UrlResearchJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}

public sealed class ContentDraftJobChannel(ILogger<ContentDraftJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("ContentDraftJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}

public sealed class ApplySourcesJobChannel(ILogger<ApplySourcesJobChannel> logger)
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Notify()
    {
        if (!_channel.Writer.TryWrite(0))
            logger.LogWarning("ApplySourcesJobChannel at capacity — notification dropped; job will be picked up on next startup drain");
    }

    public ChannelReader<byte> Reader => _channel.Reader;
}
