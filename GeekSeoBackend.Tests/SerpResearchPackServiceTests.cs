using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class SerpResearchPackServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async Task BuildAsync_ReturnsPackWithFiveClosingFaqs()
    {
        var serp = BuildSerpResult();
        var service = new SerpResearchPackService(
            new StubSerpProvider(serp),
            new StubSerpCacheRepository(),
            new StubCrawlerProvider(),
            new CompetitorCrawlService(new StubCrawlerProvider(), new StubCompetitorPageRepository()));

        var result = await service.BuildAsync(Guid.NewGuid(), new UrlAnalyzerResearchRequest
        {
            Url = "https://example.com/quickbooks-automation",
            Location = "United States",
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("https://example.com/quickbooks-automation", result.Value!.Meta.SourceUrl);
        Assert.Equal("QuickBooks automation guide", result.Value.Meta.Keyword);
        Assert.Contains("Example", result.Value.Meta.BusinessContext, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, result.Value!.ClosingFaqQuestions.Count);
        Assert.Equal("live", result.Value.Meta.DataQuality);
        Assert.True(result.Value.Paa.Count >= 2);
        Assert.True(result.Value.Pasf.Count >= 1);
        Assert.Equal(10, result.Value.Organic.Count);
    }

    [Fact]
    public async Task BuildAsync_FailsWhenSerpUnavailable()
    {
        var service = new SerpResearchPackService(
            new StubSerpProvider(null, "SERP provider unavailable"),
            new StubSerpCacheRepository(),
            new StubCrawlerProvider(),
            new CompetitorCrawlService(new StubCrawlerProvider(), new StubCompetitorPageRepository()));

        var result = await service.BuildAsync(Guid.NewGuid(), new UrlAnalyzerResearchRequest
        {
            Url = "https://example.com/test-page",
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("SERP", result.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static SerpResult BuildSerpResult() => new()
    {
        Keyword = "QuickBooks automation",
        Location = "United States",
        OrganicResults = Enumerable.Range(1, 10).Select(i => new SerpOrganicResult
        {
            Position = i,
            Url = $"https://example{i}.com/guide",
            Title = $"Guide to QuickBooks automation {i}",
            Snippet = "Learn how to automate QuickBooks workflows for small business accounting.",
            Domain = $"example{i}.com",
        }).ToList(),
        PeopleAlsoAsk =
        [
            new PeopleAlsoAskResult { Question = "What is QuickBooks automation?", Answer = "Automation in QuickBooks..." },
            new PeopleAlsoAskResult { Question = "How much does QuickBooks automation cost?", Answer = "Pricing varies..." },
            new PeopleAlsoAskResult { Question = "Is QuickBooks automation worth it?", Answer = "For many SMBs..." },
        ],
        RelatedSearches = ["quickbooks workflow automation", "quickbooks integrations"],
        FeaturedSnippetText = "QuickBooks automation connects apps and rules to reduce manual bookkeeping.",
        Features = new SerpFeatures { HasFeaturedSnippet = true, HasPeopleAlsoAsk = true },
        FetchedAt = DateTimeOffset.UtcNow,
    };

    private sealed class StubSerpProvider(SerpResult? serp, string? error = null) : ISerpProvider
    {
        public string ProviderName => "stub";

        public Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default) =>
            serp is null
                ? Task.FromResult(Result<SerpResult>.Failure(error ?? "failed"))
                : Task.FromResult(Result<SerpResult>.Success(serp));
    }

    private sealed class StubSerpCacheRepository : ISerpCacheRepository
    {
        public Task<Result<SeoSerpResult?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult?>.Success(null));

        public Task<Result<SeoSerpResult>> UpsertAsync(
            string keyword, string location, string languageCode,
            SerpResult serp, SerpBenchmarksPayload benchmarks,
            CancellationToken ct = default)
        {
            var row = SerpResultStore.ToEphemeralRow(serp, benchmarks, languageCode, 90);
            return Task.FromResult(Result<SeoSerpResult>.Success(row));
        }

        public Task<Result> DeleteAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class StubCrawlerProvider : ICrawlerProvider
    {
        public string ProviderName => "stub";

        public Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(Result<PageContent>.Success(new PageContent
            {
                Url = url,
                FullText = "Sample competitor page content about QuickBooks automation workflows.",
                MetaTitle = "QuickBooks automation guide",
                MetaDescription = "We help small businesses automate QuickBooks workflows and integrations.",
                WordCount = 1500,
                Headings =
                [
                    new PageHeading { Level = 1, Text = "QuickBooks automation guide" },
                    new PageHeading { Level = 2, Text = "Why automate QuickBooks" },
                    new PageHeading { Level = 3, Text = "Common integrations" },
                ],
                HasStructuredData = true,
                StructuredDataTypes = ["FAQPage", "Article"],
                CrawledAt = DateTimeOffset.UtcNow,
            }));
    }

    private sealed class StubCompetitorPageRepository : ICompetitorPageRepository
    {
        public Task<Result<IReadOnlyList<SeoCompetitorPage>>> GetBySerpResultAsync(Guid serpResultId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoCompetitorPage>>.Success([]));

        public Task<Result<SeoCompetitorPage>> UpsertAsync(Guid serpResultId, PageContent page, CancellationToken ct = default)
        {
            var headings = page.Headings.Select(h => h.Text).ToList();
            return Task.FromResult(Result<SeoCompetitorPage>.Success(new SeoCompetitorPage
            {
                Id = Guid.NewGuid(),
                SerpResultId = serpResultId,
                Url = page.Url,
                Domain = new Uri(page.Url).Host,
                MetaTitle = page.MetaTitle,
                ContentText = page.FullText,
                WordCount = page.WordCount,
                HeadingsJson = JsonSerializer.Serialize(headings, JsonOptions),
                StructuredDataTypesJson = JsonSerializer.Serialize(page.StructuredDataTypes, JsonOptions),
                HasStructuredData = page.HasStructuredData,
                CrawledAt = page.CrawledAt,
                ExpiresAt = page.CrawledAt.AddDays(7),
            }));
        }
    }
}
