using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class PublishedContentAuditServiceTests
{
    [Fact]
    public void BuildMetrics_flags_critical_decay_when_clicks_drop_sharply()
    {
        var recent = new Dictionary<string, PublishedContentAuditService.PageAggregate>(StringComparer.OrdinalIgnoreCase)
        {
            ["https://example.com/a"] = new() { Clicks = 2, Impressions = 100, Position = 12 },
        };
        var baseline = new Dictionary<string, PublishedContentAuditService.PageAggregate>(StringComparer.OrdinalIgnoreCase)
        {
            ["https://example.com/a"] = new() { Clicks = 20, Impressions = 500, Position = 4 },
        };

        var metrics = PublishedContentAuditService.BuildMetrics("https://example.com/a", recent, baseline);

        Assert.Equal("critical", metrics.Status);
        Assert.True(metrics.ClicksChangePercent <= -50);
    }
}
