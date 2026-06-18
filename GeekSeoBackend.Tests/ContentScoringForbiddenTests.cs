using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;
using System.Text.Json;

namespace GeekSeoBackend.Tests;

public sealed class ContentScoringForbiddenTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DocumentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ResearchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task ProcessContentChangedAsync_uses_document_title_when_body_has_no_h1()
    {
        var serp = new ThrowingSerpProvider();
        var document = ResearchDocument();
        document.Title = "Widget repair guide for local shops";
        document.ContentHtml =
            "<h2>Overview</h2><h2>Steps</h2><h2>Tools</h2><h2>FAQ</h2>" +
            "<p>widget repair content without an h1 in the body.</p>";

        var sut = CreateScoringService(document, CompletedResearch(), serp, new TrackingContentDocumentRepository());

        var result = await sut.ProcessContentChangedAsync(
            UserId,
            DocumentId,
            document.ContentHtml,
            "widget repair");

        Assert.True(result.IsSuccess, result.Error);
        var componentsJson = JsonSerializer.Serialize(result.Value!.ScoreUpdate!.Components);
        using var parsed = JsonDocument.Parse(componentsJson);
        var titleTagScore = parsed.RootElement.GetProperty("titleTag").GetInt32();
        Assert.True(titleTagScore > 0, $"expected title tag score from document title, got {titleTagScore}");
    }

    [Fact]
    public async Task ProcessKeywordChangedAsync_forbids_live_serp_on_research_document()
    {
        var serp = new ThrowingSerpProvider();
        var sut = CreateScoringService(ResearchDocument(), CompletedResearch(), serp);

        var result = await sut.ProcessKeywordChangedAsync(
            UserId,
            DocumentId,
            "<h1>Widgets</h1><p>Body copy about widgets.</p>",
            "new keyword",
            "United States");

        Assert.False(result.IsSuccess);
        Assert.Contains("forbidden", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, serp.CallCount);
    }

    [Fact]
    public async Task ProcessContentChangedAsync_scores_recommended_terms_for_research_document()
    {
        var serp = new ThrowingSerpProvider();
        var research = CompletedResearch();
        research.ResearchedAt = DateTimeOffset.Parse("2026-06-17T12:00:00Z");
        research.RecommendedTerms =
        [
            new SeoUrlResearchTerm { UrlResearchId = ResearchId, Term = "widget", DisplayOrder = 1 },
            new SeoUrlResearchTerm { UrlResearchId = ResearchId, Term = "calibration", DisplayOrder = 2 },
        ];

        var sut = CreateScoringService(ResearchDocument(), research, serp, new TrackingContentDocumentRepository());
        var html =
            "<h1>Widget repair guide</h1>" +
            "<h2>Overview</h2><h2>Steps</h2><h2>Tools</h2><h2>FAQ</h2>" +
            "<p>widget calibration process for local repair shops with detailed guidance.</p>";

        var result = await sut.ProcessContentChangedAsync(
            UserId,
            DocumentId,
            html,
            "widget repair");

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value?.ScoreUpdate);
        Assert.Equal(0, serp.CallCount);
        Assert.NotNull(result.Value.ScoreUpdate.ResearchedAt);
        Assert.Contains("recommended SERP terms", result.Value.ScoreUpdate.ScoreContextNote, StringComparison.OrdinalIgnoreCase);

        var componentsJson = JsonSerializer.Serialize(result.Value.ScoreUpdate.Components);
        using var doc = JsonDocument.Parse(componentsJson);
        Assert.Equal(35, doc.RootElement.GetProperty("termCoverage").GetInt32());
    }

    [Fact]
    public async Task ProcessContentChangedAsync_scores_research_document_without_live_serp()
    {
        var serp = new ThrowingSerpProvider();
        var repo = new TrackingContentDocumentRepository();
        var sut = CreateScoringService(ResearchDocument(), CompletedResearch(), serp, repo);

        var result = await sut.ProcessContentChangedAsync(
            UserId,
            DocumentId,
            "<h1>Widget repair guide</h1><h2>Overview</h2><h2>Steps</h2><h2>FAQ</h2><p>widget repair content.</p>",
            "widget repair");

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value?.ScoreUpdate);
        Assert.Equal(0, serp.CallCount);
        Assert.True(repo.ScoreUpdated);
    }

    [Fact]
    public async Task RefreshCrawlForDocumentAsync_forbids_live_serp_on_research_document()
    {
        var serp = new ThrowingSerpProvider();
        var insights = new CompetitorInsightsService(
            new FakeDocumentService(ResearchDocument()),
            new FakeUrlResearchRepository(CompletedResearch()),
            new NoOpSerpCacheRepository(),
            serp,
            new CompetitorCrawlService(new FakeCrawlerProvider(), new FakeCompetitorPageRepository()),
            new FakeCompetitorPageRepository());

        var result = await insights.RefreshCrawlForDocumentAsync(UserId, DocumentId);

        Assert.False(result.IsSuccess);
        Assert.Contains("forbidden", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, serp.CallCount);
    }

    [Fact]
    public async Task GetForDocumentAsync_returns_frozen_competitors_without_live_serp()
    {
        var serp = new ThrowingSerpProvider();
        var research = CompletedResearch();
        var insights = new CompetitorInsightsService(
            new FakeDocumentService(ResearchDocument()),
            new FakeUrlResearchRepository(research),
            new NoOpSerpCacheRepository(),
            serp,
            new CompetitorCrawlService(new FakeCrawlerProvider(), new FakeCompetitorPageRepository()),
            new FakeCompetitorPageRepository());

        var result = await insights.GetForDocumentAsync(UserId, DocumentId);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Single(result.Value!.Pages);
        Assert.Equal("https://competitor.example/page", result.Value.Pages[0].Url);
        Assert.Equal(1400, result.Value.Pages[0].WordCount);
        Assert.Equal(0, serp.CallCount);
    }

    private static ContentScoringService CreateScoringService(
        SeoContentDocument document,
        SeoUrlResearch research,
        ThrowingSerpProvider serp,
        TrackingContentDocumentRepository? repo = null)
    {
        repo ??= new TrackingContentDocumentRepository();
        return new ContentScoringService(
            new FakeDocumentService(document),
            repo,
            new FakeUrlResearchRepository(research),
            new NoOpSerpCacheRepository(),
            serp,
            new CompetitorCrawlService(new FakeCrawlerProvider(), new FakeCompetitorPageRepository()),
            new FakeRichTextProvider(),
            new NoOpAiProvider());
    }

    private static SeoContentDocument ResearchDocument() => new()
    {
        Id = DocumentId,
        ProjectId = ProjectId,
        UserId = UserId,
        Title = "Widget repair",
        TargetKeyword = "widget repair",
        TargetLocation = "United States",
        ContentHtml = "<h1>Widget repair</h1>",
        UrlResearchId = ResearchId,
    };

    private static SeoUrlResearch CompletedResearch() => new()
    {
        Id = ResearchId,
        ProjectId = ProjectId,
        UserId = UserId,
        SourceUrl = "https://example.com/widget-repair",
        DerivedKeyword = "widget repair",
        SearchLocation = "United States",
        Status = "completed",
        DataQuality = "full",
        MedianWordCountTop5 = 1500,
        MedianTitleLengthTop10 = 55,
        DominantContentFormat = "guide",
        ResearchedAt = DateTimeOffset.UtcNow,
        OrganicResults =
        [
            new SeoUrlResearchOrganic
            {
                UrlResearchId = ResearchId,
                Position = 1,
                Url = "https://competitor.example/page",
                Domain = "competitor.example",
                Title = "Competitor title",
                Snippet = "Snippet",
                ContentType = "article",
            },
        ],
        Competitors =
        [
            new SeoUrlResearchCompetitor
            {
                UrlResearchId = ResearchId,
                Url = "https://competitor.example/page",
                Position = 1,
                H1 = "Competitor H1",
                EstimatedWordCount = 1400,
            },
        ],
    };

    private sealed class ThrowingSerpProvider : ISerpProvider
    {
        public int CallCount { get; private set; }
        public string ProviderName => "throwing";

        public Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
        {
            CallCount++;
            throw new InvalidOperationException("ISerpProvider must not be called for research-backed documents.");
        }
    }

    private sealed class NoOpSerpCacheRepository : ISerpCacheRepository
    {
        public Task<Result<SeoSerpResult?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult?>.Success(null));

        public Task<Result<SeoSerpResult>> UpsertAsync(
            string keyword,
            string location,
            string languageCode,
            SerpResult serp,
            SerpBenchmarksPayload benchmarks,
            CancellationToken ct = default) =>
            throw new InvalidOperationException("SERP cache upsert must not run on research path.");

        public Task<Result> DeleteAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            throw new InvalidOperationException("SERP cache delete must not run on research path.");
    }

    private sealed class FakeRichTextProvider : IRichTextProvider
    {
        public string ProviderName => "fake";

        public string ExtractPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
                .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        public int CountWords(string html) => 1500;
    }

    private sealed class NoOpAiProvider : IAIProvider
    {
        public string ProviderName => "noop";

        public Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<AIResponse>.Failure("not used"));
    }

    private sealed class FakeDocumentService(SeoContentDocument document) : IContentDocumentService
    {
        public Task<Result<SeoContentDocument>> EnsureAccessAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            documentId == document.Id
                ? Task.FromResult(Result<SeoContentDocument>.Success(document))
                : Task.FromResult(Result<SeoContentDocument>.NotFound("not found"));

        public Task<Result<SeoContentDocument>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            EnsureAccessAsync(userId, documentId, ct);

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(document));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(
            Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(
            Guid userId, Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
            Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUrlResearchRepository(SeoUrlResearch research) : IUrlResearchRepository
    {
        public Task<Result<SeoUrlResearch>> GetHeadAsync(Guid urlResearchId, CancellationToken ct = default) =>
            GetFullAsync(urlResearchId, ct);

        public Task<Result<SeoUrlResearch>> GetFullAsync(Guid urlResearchId, CancellationToken ct = default) =>
            urlResearchId == research.Id
                ? Task.FromResult(Result<SeoUrlResearch>.Success(research))
                : Task.FromResult(Result<SeoUrlResearch>.NotFound("not found"));

        public Task<Result<SeoUrlResearch>> CreateQueuedAsync(
            Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> PersistFullAsync(
            Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> UpdateStatusAsync(
            Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<int>> FailStaleRunningAsync(TimeSpan maxAge, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<bool>> TryClaimRunningAsync(Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class TrackingContentDocumentRepository : IContentDocumentRepository
    {
        public bool ScoreUpdated { get; private set; }

        public Task<Result> UpdateScoreAsync(Guid documentId, int score, string scoreComponentsJson, CancellationToken ct = default)
        {
            ScoreUpdated = true;
            return Task.FromResult(Result.Success());
        }

        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateFeaturedImageAsync(Guid documentId, string featuredImageUrl, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateAiDetectionScoreAsync(Guid documentId, decimal score, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCrawlerProvider : ICrawlerProvider
    {
        public string ProviderName => "fake";

        public Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeCompetitorPageRepository : ICompetitorPageRepository
    {
        public Task<Result<IReadOnlyList<SeoCompetitorPage>>> GetBySerpResultAsync(Guid serpResultId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoCompetitorPage>>.Success([]));

        public Task<Result<SeoCompetitorPage>> UpsertAsync(Guid serpResultId, PageContent page, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
