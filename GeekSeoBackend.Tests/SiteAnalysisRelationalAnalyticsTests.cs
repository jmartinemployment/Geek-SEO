using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalysisRelationalAnalyticsTests
{
    [Fact]
    public void BuildCoverageMatrix_MapsPillarSubtopicCounts()
    {
        var pillarId = Guid.NewGuid();
        var profile = new SiteAnalysisProfile
        {
            Id = Guid.NewGuid(),
            Pillars =
            [
                new SiteAnalysisPillar
                {
                    Id = pillarId,
                    PillarTopic = "Roof Repair",
                    PrimaryKeyword = "roof repair",
                    CoverageStatus = "partial",
                    CoverageScore = 55,
                    StrategicPriority = "expansion",
                    Subtopics =
                    [
                        new SiteAnalysisSubtopic { CoverageStatus = "covered", IsQuickWin = false },
                        new SiteAnalysisSubtopic { CoverageStatus = "gap", IsQuickWin = true },
                    ],
                },
            ],
        };

        var matrix = SiteAnalysisRelationalAnalyticsBuilder.BuildCoverageMatrix(profile);

        Assert.Single(matrix);
        Assert.Equal(1, matrix[0].CoveredSubtopics);
        Assert.Equal(2, matrix[0].TotalSubtopics);
        Assert.Equal(1, matrix[0].GapSubtopics);
        Assert.True(matrix[0].HasQuickWins);
    }

    [Fact]
    public void BuildTopicalGaps_FiltersQuickWins()
    {
        var profile = new SiteAnalysisProfile
        {
            Id = Guid.NewGuid(),
            Pillars =
            [
                new SiteAnalysisPillar
                {
                    PillarTopic = "HVAC",
                    Subtopics =
                    [
                        new SiteAnalysisSubtopic
                        {
                            Id = Guid.NewGuid(),
                            SubtopicTitle = "AC tune up",
                            TargetKeyword = "ac tune up",
                            CoverageStatus = "gap",
                            IsQuickWin = true,
                            RecommendedFormat = "how_to",
                            FixEffort = "create",
                        },
                        new SiteAnalysisSubtopic
                        {
                            Id = Guid.NewGuid(),
                            SubtopicTitle = "Furnace repair",
                            TargetKeyword = "furnace repair",
                            CoverageStatus = "gap",
                            IsQuickWin = false,
                            RecommendedFormat = "how_to",
                            FixEffort = "create",
                        },
                    ],
                },
            ],
        };

        var all = SiteAnalysisRelationalAnalyticsBuilder.BuildTopicalGaps(profile, quickWinsOnly: false);
        var quick = SiteAnalysisRelationalAnalyticsBuilder.BuildTopicalGaps(profile, quickWinsOnly: true);

        Assert.Equal(2, all.Count);
        Assert.Single(quick);
        Assert.True(quick[0].IsQuickWin);
    }
}
