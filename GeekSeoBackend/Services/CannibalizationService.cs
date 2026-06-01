using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class CannibalizationService(IGoogleDataService googleData)
{
    private const int GscMaxRows = 5000;

    public async Task<CannibalizationReport> AnalyzeAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken ct = default)
    {
        var rankings = await googleData.GetRankingsAsync(userId, projectId, startDate, endDate, GscMaxRows, ct);
        var analysis = BuildIssues(rankings.Rows);

        return new CannibalizationReport
        {
            ProjectId = projectId,
            StartDate = rankings.StartDate,
            EndDate = rankings.EndDate,
            GscRowCount = rankings.Rows.Count,
            UniqueQueryCount = analysis.UniqueQueryCount,
            MultiUrlQueryCount = analysis.MultiUrlQueryCount,
            CompetingQueryCount = analysis.Issues.Count,
            Issues = analysis.Issues,
        };
    }

    internal static CannibalizationAnalysis BuildIssues(IReadOnlyList<GoogleRankingRow> rows)
    {
        var queryGroups = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Query) && !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => r.Query.Trim().ToLowerInvariant())
            .ToList();

        var uniqueQueryCount = queryGroups.Count;
        var multiUrlQueryCount = 0;
        var competing = new List<(string Query, List<CannibalizationPage> Pages, long TotalImpressions)>();

        foreach (var g in queryGroups)
        {
            var pages = g
                .GroupBy(x => CannibalizationPageNormalizer.Normalize(x.Page))
                .Where(pg => !string.IsNullOrWhiteSpace(pg.Key))
                .Select(pg => new CannibalizationPage
                {
                    Url = pg.First().Page,
                    Impressions = pg.Sum(x => x.Impressions),
                    Clicks = pg.Sum(x => x.Clicks),
                    Position = pg.Average(x => x.Position),
                })
                .OrderByDescending(p => p.Impressions)
                .ToList();

            if (pages.Count < 2)
                continue;

            multiUrlQueryCount++;
            var totalImpressions = pages.Sum(p => p.Impressions);
            if (totalImpressions < 1)
                continue;

            competing.Add((g.First().Query, pages, totalImpressions));
        }

        var issues = competing
            .OrderByDescending(x => x.TotalImpressions)
            .Take(50)
            .Select(item =>
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
            })
            .ToList();

        return new CannibalizationAnalysis(uniqueQueryCount, multiUrlQueryCount, issues);
    }

    internal sealed record CannibalizationAnalysis(
        int UniqueQueryCount,
        int MultiUrlQueryCount,
        IReadOnlyList<CannibalizationIssue> Issues);
}
