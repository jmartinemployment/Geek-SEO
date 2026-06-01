using System.Text.Json;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using Moq;
using Xunit;

namespace GeekSeoBackend.Tests;

public sealed class EntityGapAnalyzerTests
{
    private readonly Mock<ISerpDeepCacheRepository> _mockSerpRepo = new();
    private readonly EntityGapAnalyzer _analyzer;

    public EntityGapAnalyzerTests()
    {
        _analyzer = new EntityGapAnalyzer(_mockSerpRepo.Object);
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyTopics_ReturnsEmptyList()
    {
        var topics = Array.Empty<TopicalMapTopic>();
        var projectQueries = new[] { "seo tools" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNullMainKeyword_SetsGapsAndCoverageToDefaults()
    {
        var topic = new TopicalMapTopic
        {
            Name = "NoKeyword",
            Queries = new[] { "test" },
            Coverage = "gap",
            MainKeyword = null,
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = new[] { "competitor.com" },
        };
        var topics = new[] { topic };
        var projectQueries = new[] { "seo tools" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Single(result);
        var enriched = result[0];
        Assert.Empty(enriched.EntityGaps);
        Assert.Equal(1m, enriched.EntityCoverage);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNoCompetitorDomains_SetsGapsAndCoverageToDefaults()
    {
        var topic = new TopicalMapTopic
        {
            Name = "NoCompetitors",
            Queries = new[] { "test" },
            Coverage = "gap",
            MainKeyword = "seo tools",
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = Array.Empty<string>(),
        };
        var topics = new[] { topic };
        var projectQueries = new[] { "seo" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Single(result);
        var enriched = result[0];
        Assert.Empty(enriched.EntityGaps);
        Assert.Equal(1m, enriched.EntityCoverage);
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidSerpCache_IdentifiesEntityGaps()
    {
        var serpJson = JsonSerializer.Serialize(new object[]
        {
            new { title = "Best SEO tools for keyword research", snippet = "Find the best tools for SEO analysis" },
            new { title = "Top free SEO tools available online", snippet = "Free tools for on-page optimization" },
            new { title = "SEO tools comparison: pricing and features", snippet = "Compare different keyword research platforms" },
        });

        var cacheEntry = new SeoSerpDeepCache
        {
            Keyword = "seo tools",
            Location = "US",
            ResultsJson = serpJson,
        };

        _mockSerpRepo
            .Setup(x => x.GetAsync("seo tools", "US", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(cacheEntry));

        var topic = new TopicalMapTopic
        {
            Name = "SEO Tools Overview",
            Queries = new[] { "seo tools" },
            Coverage = "partial",
            MainKeyword = "seo tools",
            Tier = TopicalTier.Pillar,
            TotalImpressions = 100,
            CompetitorDomains = new[] { "competitor1.com", "competitor2.com" },
        };
        var topics = new[] { topic };
        var projectQueries = new[] { "seo tools" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Single(result);
        var enriched = result[0];

        // EntityGaps should contain phrases from competitors not in project queries
        Assert.NotEmpty(enriched.EntityGaps);
        // "keyword research", "free tools", "optimization" appear 3+ times in competitors
        Assert.Contains("keyword research", enriched.EntityGaps);

        // EntityCoverage > 0 because project covers some entities
        Assert.True(enriched.EntityCoverage > 0);
        Assert.True(enriched.EntityCoverage <= 1);
    }

    [Fact]
    public async Task AnalyzeAsync_WithSerpCacheFailure_SetsGapsAndCoverageToDefaults()
    {
        _mockSerpRepo
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Failure("Cache miss"));

        var topic = new TopicalMapTopic
        {
            Name = "Test Topic",
            Queries = new[] { "test query" },
            Coverage = "gap",
            MainKeyword = "test query",
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = new[] { "competitor.com" },
        };
        var topics = new[] { topic };
        var projectQueries = new[] { "test query" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Single(result);
        var enriched = result[0];
        Assert.Empty(enriched.EntityGaps);
        Assert.Equal(1m, enriched.EntityCoverage);
    }

    [Fact]
    public async Task AnalyzeAsync_WithMultipleTopics_AnalyzeAllIndependently()
    {
        var serpJson1 = JsonSerializer.Serialize(new object[]
        {
            new { title = "Best keyword research tools", snippet = "Find SEO platforms" },
            new { title = "Keyword research software comparison", snippet = "Compare tools for keyword analysis" },
            new { title = "Keyword research best practices guide", snippet = "Learn keyword research methods" },
        });

        var serpJson2 = JsonSerializer.Serialize(new object[]
        {
            new { title = "Link building strategy guide", snippet = "Backlink acquisition tactics" },
            new { title = "Link building tools and software", snippet = "Automate link building" },
            new { title = "Link building best practices 2024", snippet = "Natural link building methods" },
        });

        _mockSerpRepo
            .Setup(x => x.GetAsync("keyword research", "US", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(new SeoSerpDeepCache { Keyword = "keyword research", ResultsJson = serpJson1 }));

        _mockSerpRepo
            .Setup(x => x.GetAsync("link building", "US", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(new SeoSerpDeepCache { Keyword = "link building", ResultsJson = serpJson2 }));

        var topics = new[]
        {
            new TopicalMapTopic
            {
                Name = "Keyword Research Tools",
                Queries = new[] { "keyword research" },
                Coverage = "partial",
                MainKeyword = "keyword research",
                Tier = TopicalTier.Cluster,
                TotalImpressions = 50,
                CompetitorDomains = new[] { "comp1.com" },
            },
            new TopicalMapTopic
            {
                Name = "Link Building Strategy",
                Queries = new[] { "link building" },
                Coverage = "gap",
                MainKeyword = "link building",
                Tier = TopicalTier.Cluster,
                TotalImpressions = 30,
                CompetitorDomains = new[] { "comp2.com" },
            },
        };

        var projectQueries = new[] { "keyword research", "link building" };

        var result = await _analyzer.AnalyzeAsync(topics, projectQueries, "US", CancellationToken.None);

        Assert.Equal(2, result.Count);

        // First topic should have gaps related to keyword research
        Assert.NotEmpty(result[0].EntityGaps);
        Assert.True(result[0].EntityCoverage > 0);

        // Second topic should have gaps related to link building
        Assert.NotEmpty(result[1].EntityGaps);
        Assert.True(result[1].EntityCoverage > 0);
    }

    [Fact]
    public async Task AnalyzeAsync_EntityGaps_ContainOnlyPhrasesWith3PlusOccurrences()
    {
        var serpJson = JsonSerializer.Serialize(new object[]
        {
            new { title = "First rare phrase occurrence", snippet = "Standard content" },
            new { title = "Common entity common entity common entity", snippet = "Repeated common entity here" },
            new { title = "Another common entity in title", snippet = "common entity appears everywhere" },
        });

        _mockSerpRepo
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(new SeoSerpDeepCache { ResultsJson = serpJson }));

        var topic = new TopicalMapTopic
        {
            Name = "Test",
            Queries = new[] { "test" },
            Coverage = "gap",
            MainKeyword = "test",
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = new[] { "comp.com" },
        };

        var result = await _analyzer.AnalyzeAsync(new[] { topic }, new[] { "test" }, "US", CancellationToken.None);

        var enriched = result[0];
        // "rare" and "phrase" should NOT be in gaps (< 3 occurrences)
        // "common" and "entity" should be in gaps (3+ occurrences)
        Assert.DoesNotContain("rare phrase", enriched.EntityGaps);
        Assert.DoesNotContain("rare", enriched.EntityGaps);
    }

    [Fact]
    public async Task AnalyzeAsync_EntityGaps_LimitedToTop20()
    {
        var titles = Enumerable.Range(1, 30)
            .Select(i => new { title = $"Entity {i} content description summary", snippet = $"Description for entity number {i}" })
            .ToArray();

        var serpJson = JsonSerializer.Serialize((object)titles);

        _mockSerpRepo
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(new SeoSerpDeepCache { ResultsJson = serpJson }));

        var topic = new TopicalMapTopic
        {
            Name = "Test",
            Queries = new[] { "test" },
            Coverage = "gap",
            MainKeyword = "test",
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = new[] { "comp.com" },
        };

        var result = await _analyzer.AnalyzeAsync(new[] { topic }, new[] { "test" }, "US", CancellationToken.None);

        var enriched = result[0];
        Assert.True(enriched.EntityGaps.Count <= 20);
    }

    [Fact]
    public async Task AnalyzeAsync_EntityCoverage_IsDecimalBetweenZeroAndOne()
    {
        var serpJson = JsonSerializer.Serialize(new object[]
        {
            new { title = "Competitor content here", snippet = "More competitor text" },
            new { title = "Additional competitor info", snippet = "Extra competitor details" },
            new { title = "Another competitor entry", snippet = "More text from competitors" },
        });

        _mockSerpRepo
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SeoSerpDeepCache>.Success(new SeoSerpDeepCache { ResultsJson = serpJson }));

        var topic = new TopicalMapTopic
        {
            Name = "Test",
            Queries = new[] { "test" },
            Coverage = "gap",
            MainKeyword = "test",
            Tier = TopicalTier.Article,
            TotalImpressions = 0,
            CompetitorDomains = new[] { "comp.com" },
        };

        var result = await _analyzer.AnalyzeAsync(new[] { topic }, new[] { "test" }, "US", CancellationToken.None);

        var enriched = result[0];
        Assert.True(enriched.EntityCoverage >= 0m);
        Assert.True(enriched.EntityCoverage <= 1m);
    }
}
