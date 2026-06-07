using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

/// <summary>
/// Builds coverage/gap read models from relational pillar rows when Dapper analytics routes fail.
/// </summary>
internal static class NicheRelationalAnalyticsBuilder
{
    public static IReadOnlyList<PillarCoverageMatrix> BuildCoverageMatrix(NicheProfile profile)
    {
        return profile.Pillars
            .OrderBy(p => p.DisplayOrder)
            .Select(p =>
            {
                var subtopics = p.Subtopics.ToList();
                var gapSubtopics = subtopics.Count(s => s.CoverageStatus == "gap");
                var coveredSubtopics = subtopics.Count(s => s.CoverageStatus == "covered");
                var hasQuickWins = subtopics.Any(s => s.IsQuickWin);
                return new PillarCoverageMatrix(
                    p.Id,
                    p.PillarTopic,
                    p.PrimaryKeyword,
                    p.SearchVolume,
                    p.KeywordDifficulty,
                    p.CoverageScore,
                    coveredSubtopics,
                    subtopics.Count,
                    gapSubtopics,
                    p.CoverageStatus,
                    p.StrategicPriority,
                    hasQuickWins);
            })
            .ToList();
    }

    public static IReadOnlyList<TopicalGapSummary> BuildTopicalGaps(
        NicheProfile profile,
        bool quickWinsOnly)
    {
        var rows = profile.Pillars
            .OrderBy(p => p.DisplayOrder)
            .SelectMany(p => p.Subtopics
                .Where(s => s.CoverageStatus == "gap")
                .Select(s => new TopicalGapSummary(
                    s.Id,
                    p.PillarTopic,
                    s.SubtopicTitle,
                    s.TargetKeyword,
                    s.SearchVolume,
                    s.KeywordDifficulty,
                    s.IsQuickWin,
                    s.RecommendedFormat,
                    s.FixEffort)))
            .ToList();

        return quickWinsOnly ? rows.Where(r => r.IsQuickWin).ToList() : rows;
    }
}
