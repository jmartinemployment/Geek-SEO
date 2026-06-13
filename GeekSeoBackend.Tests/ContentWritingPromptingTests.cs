using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentWritingPromptingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task GenerateBriefAsync_EnrichesBriefFromNicheAnalyzerContext()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var keyword = "zapier quickbooks integration";
        var location = "Palm Beach County, FL";

        var project = new SeoProject
        {
            Id = projectId,
            UserId = userId,
            Name = "Geek @ Your Spot",
            Url = "https://geekatyourspot.com",
            DefaultLocation = location,
            BusinessAddress = "West Palm Beach, FL",
        };

        var serpRow = new SeoSerpResult
        {
            Id = Guid.NewGuid(),
            Keyword = keyword,
            Location = location,
            ResultsJson = JsonSerializer.Serialize(
                new SerpBenchmarksPayload
                {
                    AvgWordCount = 1800,
                    AvgTitleLength = 58,
                    BenchmarkQuality = "good",
                    OrganicResults =
                    [
                        new SerpOrganicResult
                        {
                            Position = 1,
                            Url = "https://competitor-one.example/zapier-quickbooks",
                            Title = "Zapier QuickBooks Integration Guide",
                            Snippet = "Connect Zapier to QuickBooks with mapped invoice and payment steps.",
                            Domain = "competitor-one.example",
                        },
                    ],
                },
                JsonOptions),
            PeopleAlsoAskJson = JsonSerializer.Serialize(
                new List<PeopleAlsoAskResult>
                {
                    new()
                    {
                        Question = "How much does Zapier QuickBooks integration cost?",
                        Answer = "Most teams start with Zapier task-based pricing plus QuickBooks admin time.",
                    },
                },
                JsonOptions),
            RelatedSearchesJson = JsonSerializer.Serialize(
                new List<string>
                {
                    "quickbooks automation consultant",
                    "zapier accounting workflow",
                },
                JsonOptions),
            FetchedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };

        var latestProfile = new NicheProfile
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            PrimaryNiche = "Business process automation for South Florida SMBs",
            NicheTags = ["South Florida", "QuickBooks", "Zapier"],
            Pillars =
            [
                new NichePillar
                {
                    Id = Guid.NewGuid(),
                    PillarTopic = "QuickBooks Automation",
                    PillarSlug = "quickbooks-automation",
                    PrimaryKeyword = "quickbooks automation consultant",
                    SearchIntent = "commercial",
                    ContentAngle = "technical_workflow",
                    DisplayOrder = 0,
                },
            ],
            Competitors =
            [
                new NicheCompetitor
                {
                    Id = Guid.NewGuid(),
                    Domain = "competitor-one.example",
                    SerpPresence = 4,
                },
                new NicheCompetitor
                {
                    Id = Guid.NewGuid(),
                    Domain = "competitor-two.example",
                    SerpPresence = 3,
                },
            ],
        };

        var service = new ContentBriefService(
            new FakeProjectRepository(project),
            new FakeSerpCacheRepository(serpRow),
            new FakeSerpProvider(),
            new FakeAiProvider("[\"Zapier routing\",\"QuickBooks invoice sync\",\"webhook retry policy\"]"),
            new FakeNicheProfileRepository(latestProfile),
            new FakeNicheAnalyticsRepository(
                [
                    new TopicalGapSummary(
                        Guid.NewGuid(),
                        "QuickBooks Automation",
                        "Zapier QuickBooks error handling",
                        keyword,
                        320,
                        24m,
                        true,
                        "how_to",
                        "create"),
                ]));

        var result = await service.GenerateBriefAsync(userId, new GenerateBriefRequest
        {
            ProjectId = projectId,
            Keyword = keyword,
            Location = location,
        });

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("Four Phase Methodology", result.Value.Methodology.Name);
        Assert.Equal(
            ["Business Objectives", "Data Quality Assessment", "Tech Selection", "Pilot Implementation Strategy"],
            result.Value.Methodology.Phases);
        Assert.Equal("QuickBooks Automation", result.Value.NicheContext.MatchedPillar);
        Assert.Contains("competitor-one.example", result.Value.CompetitorDomains);
        Assert.Contains("Palm Beach County, FL", result.Value.GeoAnchorNodes);
        Assert.Contains("West Palm Beach, FL", result.Value.GeoAnchorNodes);
        Assert.Equal("TechArticle", result.Value.SchemaBlueprint.PrimaryType);
        Assert.Contains("FAQPage", result.Value.SchemaBlueprint.AdditionalTypes);
        Assert.Contains("Zapier", result.Value.SchemaBlueprint.SoftwareEntities);
        Assert.Contains("QuickBooks", result.Value.SchemaBlueprint.SoftwareEntities);
        Assert.Contains(
            result.Value.ReviewChecklist,
            item => item.Contains("software versions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ArticleSchemaBuilder_BuildScripts_IncludesTechArticleSoftwareAndFaqSchema()
    {
        var brief = new ContentBrief
        {
            Keyword = "zapier quickbooks integration",
            Location = "Palm Beach County, FL",
            TargetWordCount = 1800,
            SchemaBlueprint = new SchemaBlueprint
            {
                PrimaryType = "TechArticle",
                AdditionalTypes = ["FAQPage"],
                SoftwareEntities = ["Zapier", "QuickBooks"],
                AboutEntities = ["Business Process Automation", "South Florida"],
            },
            PeopleAlsoAsk =
            [
                "How much does Zapier QuickBooks integration cost?",
            ],
            AuthorOrganizationName = "Geek @ Your Spot",
            AuthorOrganizationUrl = "https://geekatyourspot.com",
        };

        var articleHtml = """
            <h1>Zapier QuickBooks Integration</h1>
            <p>Zapier QuickBooks integration connects accounting workflows, invoice sync, and follow-up automation.</p>
            <h2>How much does Zapier QuickBooks integration cost?</h2>
            <p>Most projects combine Zapier subscription costs with implementation time for mapping, testing, and exception handling.</p>
            """;

        var withSchema = ArticleSchemaBuilder.AppendSchemaScripts(articleHtml, brief, "Zapier QuickBooks Integration");

        Assert.Contains("\"@type\":\"TechArticle\"", withSchema);
        Assert.Contains("\"@type\":\"SoftwareApplication\"", withSchema);
        Assert.Contains("\"name\":\"Zapier\"", withSchema);
        Assert.Contains("\"name\":\"QuickBooks\"", withSchema);
        Assert.Contains("\"@type\":\"FAQPage\"", withSchema);
        Assert.Contains("How much does Zapier QuickBooks integration cost?", withSchema);
    }

    [Fact]
    public void ArticlePromptBuilder_BuildDraftUserPrompt_ContainsMethodologyEeatAndGeoInstructions()
    {
        var brief = new ContentBrief
        {
            Keyword = "quickbooks automation consultant",
            Location = "Palm Beach County, FL",
            TargetWordCount = 1600,
            RecommendedTerms = ["QuickBooks", "Zapier", "webhook routing"],
            SuggestedHeadings = ["Business objectives", "Pilot implementation strategy"],
            Methodology = new WritingMethodologySpec(
                "Four Phase Methodology",
                ["Business Objectives", "Data Quality Assessment", "Tech Selection", "Pilot Implementation Strategy"]),
            DirectAnswerBlocks =
            [
                new DirectAnswerBlockSpec("Direct answer", "Open with a short definition and business outcome."),
            ],
            TechnicalEvidenceRequirements =
            [
                "Include sanitized code or webhook examples when the topic is technical.",
            ],
            GeoAnchorNodes = ["Palm Beach County compliance", "South Florida logistics hubs"],
            SchemaBlueprint = new SchemaBlueprint
            {
                PrimaryType = "TechArticle",
                AdditionalTypes = ["FAQPage"],
                SoftwareEntities = ["QuickBooks", "Zapier"],
            },
            ReviewChecklist =
            [
                "Verify software versions and code logic before publication.",
            ],
        };

        var prompt = ArticlePromptBuilder.BuildDraftUserPrompt(new WritingDraftRequest
        {
            Keyword = brief.Keyword,
            Brief = brief,
            Outline = "<h2>Business objectives</h2><h2>Pilot implementation strategy</h2>",
            TargetWordCount = 1600,
            Title = "QuickBooks Automation Consultant",
        });

        Assert.Contains("Business Objectives", prompt);
        Assert.Contains("Data Quality Assessment", prompt);
        Assert.Contains("Pilot Implementation Strategy", prompt);
        Assert.Contains("sanitized code", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Palm Beach County", prompt);
        Assert.Contains("JSON-LD", prompt);
        Assert.Contains("FAQPage", prompt);
    }

    private sealed class FakeProjectRepository(SeoProject project) : IProjectRepository
    {
        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoProject>>.Success([project]));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoProject>.Success(project));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoProject>.Success(project));

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSerpCacheRepository(SeoSerpResult serpRow) : ISerpCacheRepository
    {
        public Task<Result<SeoSerpResult?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult?>.Success(serpRow));

        public Task<Result<SeoSerpResult>> UpsertAsync(string keyword, string location, string languageCode, SerpResult serp, SerpBenchmarksPayload benchmarks, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(string keyword, string location, string languageCode, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSerpProvider : ISerpProvider
    {
        public string ProviderName => "fake";

        public Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAiProvider(string content) : IAIProvider
    {
        public string ProviderName => "fake";

        public Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<AIResponse>.Success(new AIResponse
            {
                Content = content,
                Model = "fake-model",
                InputTokens = 0,
                OutputTokens = 0,
                StopReason = "end_turn",
            }));
    }

    private sealed class FakeNicheProfileRepository(NicheProfile? latestProfile) : INicheProfileRepository
    {
        public Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(Guid profileId, bool includeFusion, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<NicheProfile?>.Success(latestProfile));
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStatusAsync(Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0, string? errorMessage = null, NicheAnalysisStepLogEntry? stepLogEntry = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateProfileSummaryAsync(Guid profileId, NicheProfileSummaryPatch summary, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdatePhaseStatusAsync(Guid profileId, NichePhaseStatusPatch patch, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkUpsertTopicCandidatesAsync(Guid profileId, IReadOnlyList<NicheTopicCandidateBulkUpsert> candidates, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheTopicCandidateListResult>> GetTopicCandidatesAsync(Guid profileId, int page, int pageSize, bool? selectedOnly, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveAnalysisResultsAsync(Guid profileId, NicheAnalysisSaveRequest results, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateCompetitorInsightsAsync(NicheCompetitor competitor, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status, NicheAnalysisStepLogEntry? entry = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> InvalidateDownstreamStepsAsync(Guid profileId, IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeNicheAnalyticsRepository(IReadOnlyList<TopicalGapSummary> gaps) : INicheAnalyticsDapperRepository
    {
        public Task<Result<NicheProfileSummary?>> GetProfileSummaryAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<PillarCoverageMatrix>>> GetCoverageMatrixAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<TopicalGapSummary>>> GetTopicalGapsAsync(Guid profileId, bool quickWinsOnly = false, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<TopicalGapSummary>>.Success(gaps));
        public Task<Result<IReadOnlyList<AuthorityProgressPoint>>> GetAuthorityProgressAsync(Guid projectId, int months = 12, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<CompetitorNicheOverlap>>> GetCompetitorOverlapAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<EntityCoverageReport>>> GetEntityCoverageAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
