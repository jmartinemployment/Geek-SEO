using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class CannibalizationService(IGoogleDataService googleData)
{
    public async Task<CannibalizationReport> AnalyzeAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken ct = default)
    {
        var rankings = await googleData.GetRankingsAsync(userId, projectId, startDate, endDate, 1000, ct);
        var issues = BuildIssues(rankings.Rows);

        return new CannibalizationReport
        {
            ProjectId = projectId,
            StartDate = rankings.StartDate,
            EndDate = rankings.EndDate,
            Issues = issues,
        };
    }

    internal static IReadOnlyList<CannibalizationIssue> BuildIssues(IReadOnlyList<GoogleRankingRow> rows)
    {
        var grouped = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Query) && !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => r.Query.Trim().ToLowerInvariant())
            .Select(g =>
            {
                var pages = g
                    .GroupBy(x => x.Page)
                    .Select(pg => new CannibalizationPage
                    {
                        Url = pg.Key,
                        Impressions = pg.Sum(x => x.Impressions),
                        Clicks = pg.Sum(x => x.Clicks),
                        Position = pg.Average(x => x.Position),
                    })
                    .OrderByDescending(p => p.Impressions)
                    .ToList();

                return new
                {
                    Query = g.First().Query,
                    Pages = pages,
                    TotalImpressions = pages.Sum(p => p.Impressions),
                };
            })
            .Where(x => x.Pages.Count >= 2 && x.TotalImpressions > 10)
            .OrderByDescending(x => x.TotalImpressions)
            .Take(50)
            .ToList();

        return grouped.Select(item =>
        {
            var spread = item.Pages.Max(p => p.Impressions) - item.Pages.Min(p => p.Impressions);
            var severity = item.TotalImpressions > 500 ? "high" : item.TotalImpressions > 100 ? "medium" : "low";
            var winner = item.Pages[0];
            return new CannibalizationIssue
            {
                Query = item.Query,
                Pages = item.Pages,
                TotalImpressions = item.TotalImpressions,
                Severity = severity,
                Recommendation =
                    $"Consolidate ranking signals onto {winner.Url} (top impressions). " +
                    $"Add canonical or merge content; redirect weaker URLs if they duplicate intent. " +
                    $"Impression spread: {spread}.",
            };
        }).ToList();
    }
}
