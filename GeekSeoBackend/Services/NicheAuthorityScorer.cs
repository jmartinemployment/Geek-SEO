using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

public sealed class NicheAuthorityScorer
{
    private static readonly Dictionary<string, decimal> PriorityWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["must_have"] = 3m, ["high_value"] = 2m, ["expansion"] = 1m,
    };

    public void ScorePillars(IReadOnlyList<NichePillar> pillars)
    {
        foreach (var pillar in pillars)
        {
            pillar.CoverageScore = ComputePillarCoverageScore(pillar);
            pillar.StrategicPriority = DetermineStrategicPriority(pillar);
            MarkQuickWins(pillar);
        }
    }

    public decimal ComputeTopicalAuthorityScore(IReadOnlyList<NichePillar> pillars)
    {
        if (pillars.Count == 0) return 0m;

        decimal weightedSum = 0m;
        decimal totalWeight = 0m;
        decimal penalty = 0m;
        decimal bonus = 0m;

        foreach (var pillar in pillars)
        {
            var weight = PriorityWeights.GetValueOrDefault(pillar.StrategicPriority, 1m);
            weightedSum += pillar.CoverageScore * weight;
            totalWeight += weight;

            if (pillar.CoverageScore == 0m) penalty += 5m;
            if (pillar.CoverageScore > 80m) bonus += 3m;
        }

        var raw = totalWeight > 0 ? weightedSum / totalWeight : 0m;
        return Math.Clamp(raw - penalty + bonus, 0m, 100m);
    }

    private static decimal ComputePillarCoverageScore(NichePillar pillar)
    {
        if (pillar.RequiredSubtopicCount == 0) return 0m;

        var breadth = (decimal)pillar.CoveredSubtopicCount / pillar.RequiredSubtopicCount * 60m;

        var pages = pillar.ExistingPages;
        var avgRelevance = pages.Count > 0
            ? pages.Average(p => p.RelevanceScore)
            : 0m;
        var quality = avgRelevance * 0.25m;

        // Entity coverage approximated from page quality when no entity data available
        var entityCoverage = avgRelevance * 0.15m;

        return Math.Clamp(breadth + quality + entityCoverage, 0m, 100m);
    }

    private static string DetermineStrategicPriority(NichePillar pillar)
    {
        // must_have: pillar tier (depth=0/1), or high-volume low-difficulty gap, or partial coverage
        if (pillar.Source is "schema" or "sitemap") return "must_have";
        if (pillar.SearchVolume > 500 && pillar.KeywordDifficulty < 40 && pillar.CoverageStatus == "gap")
            return "must_have";
        if (pillar.CoverageStatus == "partial") return "must_have";

        // high_value: cluster tier + any search volume
        if (pillar.SearchVolume > 100) return "high_value";

        return "expansion";
    }

    private static void MarkQuickWins(NichePillar pillar)
    {
        foreach (var sub in pillar.Subtopics)
        {
            sub.IsQuickWin = sub.KeywordDifficulty < 35m
                && sub.CoverageStatus == "gap"
                && sub.SearchVolume > 100;
        }
    }

    public static string DetermineCompetitionLevel(IReadOnlyList<NicheCompetitor> competitors)
    {
        if (competitors.Count == 0) return "low";
        var dominantCount = competitors.Count(c => c.StrengthAssessment == "dominant");
        var strongCount = competitors.Count(c => c.StrengthAssessment == "strong");
        if (dominantCount > 0) return "very_high";
        if (strongCount >= 2) return "high";
        if (competitors.Count >= 3) return "medium";
        return "low";
    }

    public static string DetermineCoverageQuality(decimal relevanceScore) => relevanceScore switch
    {
        < 40m => "thin",
        < 70m => "adequate",
        _ => "strong",
    };
}
