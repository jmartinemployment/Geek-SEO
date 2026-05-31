using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class PublishedContentAuditService(IGoogleDataService googleData, IProjectRepository projects)
{
    public async Task<PublishedContentAuditReport> AnalyzeAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");

        // GSC data lags ~3 days; compare recent 7d vs prior 21d baseline.
        var recentEnd = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        var recentStart = recentEnd.AddDays(-6);
        var baselineEnd = recentStart.AddDays(-1);
        var baselineStart = baselineEnd.AddDays(-20);

        var recentTask = googleData.GetRankingsAsync(userId, projectId, recentStart, recentEnd, 1000, ct);
        var baselineTask = googleData.GetRankingsAsync(userId, projectId, baselineStart, baselineEnd, 1000, ct);
        await Task.WhenAll(recentTask, baselineTask);

        var recentByPage = AggregateByPage(recentTask.Result.Rows);
        var baselineByPage = AggregateByPage(baselineTask.Result.Rows);
        var allUrls = recentByPage.Keys.Union(baselineByPage.Keys, StringComparer.OrdinalIgnoreCase).ToList();

        var pages = allUrls
            .Select(url => BuildMetrics(url, recentByPage, baselineByPage))
            .Where(p => p.RecentImpressions > 0 || p.BaselineImpressions > 0)
            .OrderByDescending(p => p.BaselineImpressions)
            .Take(100)
            .ToList();

        return new PublishedContentAuditReport
        {
            ProjectId = projectId,
            RecentStartDate = recentStart.ToString("yyyy-MM-dd"),
            RecentEndDate = recentEnd.ToString("yyyy-MM-dd"),
            BaselineStartDate = baselineStart.ToString("yyyy-MM-dd"),
            BaselineEndDate = baselineEnd.ToString("yyyy-MM-dd"),
            Pages = pages,
            DecayingCount = pages.Count(p => p.Status is "decaying" or "critical"),
        };
    }

    internal static PublishedPageMetrics BuildMetrics(
        string url,
        IReadOnlyDictionary<string, PageAggregate> recentByPage,
        IReadOnlyDictionary<string, PageAggregate> baselineByPage)
    {
        recentByPage.TryGetValue(url, out var recent);
        baselineByPage.TryGetValue(url, out var baseline);

        var recentClicks = recent?.Clicks ?? 0;
        var baselineClicks = baseline?.Clicks ?? 0;
        var recentImpressions = recent?.Impressions ?? 0;
        var baselineImpressions = baseline?.Impressions ?? 0;
        var recentPosition = recent?.Position ?? 0;
        var baselinePosition = baseline?.Position ?? 0;

        var clicksChange = baselineClicks > 0
            ? (recentClicks - baselineClicks) / (double)baselineClicks * 100
            : recentClicks > 0 ? 100 : 0;

        var positionChange = baselinePosition > 0 && recentPosition > 0
            ? recentPosition - baselinePosition
            : 0;

        var isDecaying = (baselineClicks >= 5 && clicksChange <= -25)
            || (baselinePosition > 0 && recentPosition > 0 && positionChange >= 5);

        var isCritical = (baselineClicks >= 10 && clicksChange <= -50)
            || (baselinePosition > 0 && recentPosition > 0 && positionChange >= 10);

        var status = isCritical ? "critical" : isDecaying ? "decaying" : "stable";
        var recommendation = status switch
        {
            "critical" =>
                "Traffic or rankings dropped sharply — refresh content, update the title/meta, and check for cannibalization.",
            "decaying" =>
                "Performance is slipping vs the prior period — review freshness, internal links, and SERP intent match.",
            _ => "Performance is stable — monitor weekly and expand topical coverage.",
        };

        return new PublishedPageMetrics
        {
            Url = url,
            RecentClicks = recentClicks,
            BaselineClicks = baselineClicks,
            RecentImpressions = recentImpressions,
            BaselineImpressions = baselineImpressions,
            RecentPosition = recentPosition,
            BaselinePosition = baselinePosition,
            ClicksChangePercent = Math.Round(clicksChange, 1),
            PositionChange = Math.Round(positionChange, 1),
            Status = status,
            Recommendation = recommendation,
        };
    }

    internal static Dictionary<string, PageAggregate> AggregateByPage(IReadOnlyList<GoogleRankingRow> rows) =>
        rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => r.Page.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new PageAggregate
                {
                    Clicks = g.Sum(x => x.Clicks),
                    Impressions = g.Sum(x => x.Impressions),
                    Position = g.Average(x => x.Position),
                },
                StringComparer.OrdinalIgnoreCase);

    internal sealed record PageAggregate
    {
        public long Clicks { get; init; }
        public long Impressions { get; init; }
        public double Position { get; init; }
    }
}
