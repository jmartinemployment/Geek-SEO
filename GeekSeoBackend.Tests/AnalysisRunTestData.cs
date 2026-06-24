using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

internal static class AnalysisRunTestData
{
    internal static readonly Guid RunId = Guid.Parse("7a9c36d8-0ecf-4c36-9387-5d02c28de201");
    internal static readonly Guid ProjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    internal static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    internal static readonly Guid SiteProfileId = Guid.Parse("98235522-ce3e-44d2-ab47-5370786cc692");
    internal static readonly Guid DocumentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    internal static ContentWriterSerpExport CompletedExport() => new()
    {
        RunId = RunId,
        ProjectId = ProjectId,
        Keyword = "widget repair",
        TargetSiteUrl = "https://example.com/widget-repair",
        Status = "completed",
        SerpSeResultsCount = 1_250_000,
        Serp =
        [
            new ContentWriterSerpItem
            {
                Position = 1,
                Type = "organic",
                Title = "Widget Repair Services",
                Url = "https://c1.com",
                Domain = "c1.com",
                Snippet = "Professional widget repair for homes and businesses with calibration support.",
            },
            new ContentWriterSerpItem
            {
                Position = 2,
                Type = "organic",
                Title = "Local Widget Experts",
                Url = "https://c2.com",
                Domain = "c2.com",
                Snippet = "Same-day widget repair with warranty.",
            },
            new ContentWriterSerpItem
            {
                Position = 3,
                Type = "organic",
                Title = "Widget calibration guide",
                Url = "https://c3.com",
                Domain = "c3.com",
                Snippet = "How to calibrate widgets at home.",
            },
            new ContentWriterSerpItem
            {
                Position = 4,
                Type = "related_searches",
                RelatedQuestions =
                [
                    "widget repair cost",
                    "how long does widget repair take?",
                    "best widget repair company",
                ],
            },
            new ContentWriterSerpItem
            {
                Position = 5,
                Type = "people_also_ask",
                RelatedQuestions = ["Is widget repair worth it?"],
                Snippet = "Widget repair is often cheaper than replacement when parts are available.",
            },
        ],
    };

    /// <summary>Realistic export shape from analysis_runs (PASF-only SERP, no people_also_ask rows).</summary>
    internal static ContentWriterSerpExport MarketResearchPasfExport() => new()
    {
        RunId = RunId,
        ProjectId = ProjectId,
        Keyword = "ai market research tools",
        TargetSiteUrl = "https://example.com",
        Status = "completed",
        SerpSeResultsCount = 2_400_000_000,
        Serp =
        [
            new ContentWriterSerpItem
            {
                Position = 1,
                Type = "organic",
                Title = "10 AI Market Research Tools & How To Use Them",
                Url = "https://competitor-a.com/tools",
                Domain = "competitor-a.com",
                Snippet = "Top AI tools for market research that speed up insights.",
            },
            new ContentWriterSerpItem
            {
                Position = 2,
                Type = "organic",
                Title = "10 Best AI Market Intelligence Tools for Enterprise 2026",
                Url = "https://competitor-b.com/enterprise",
                Domain = "competitor-b.com",
                Snippet = "Enterprise AI market intelligence platforms compared.",
            },
            new ContentWriterSerpItem
            {
                Position = 3,
                Type = "ai_overview",
                Snippet = "AI market research tools automate survey analysis and competitor tracking.",
            },
            new ContentWriterSerpItem
            {
                Position = 4,
                Type = "related_searches",
                RelatedQuestions =
                [
                    "ai in marketing analytics course",
                    "free ai tools for market research",
                    "ai market research tool",
                    "best ai for market analysis",
                    "ai market research companies",
                    "best free ai tools for market research",
                    "best ai for market research reddit",
                    "ai market research report",
                ],
            },
        ],
    };

    internal static WritingResearchContext MinimalWritingContext() =>
        ContentWriterSerpExportMapper.ToWritingResearchContext(
            CompletedExport(),
            UserId,
            "United States",
            "widget repair");

    internal static ContentWriterSiteBundle MinimalSiteBundle(Guid? projectId = null) => new()
    {
        SiteProfileId = SiteProfileId,
        GeekSeoProjectId = projectId ?? ProjectId,
        SiteUrl = "https://example.com",
        DisplayName = "Example Site",
        CapturedAt = DateTimeOffset.UtcNow,
        PrimaryNiche = "Widget services",
        BusinessSummary = "Widget repair and calibration for local businesses.",
    };

    internal static SeoContentDocument FrozenResearchDocument(
        ContentWriterSerpExport? export = null,
        string? targetKeyword = null)
    {
        export ??= CompletedExport();
        var keyword = targetKeyword ?? export.Keyword;
        return new SeoContentDocument
        {
            Id = DocumentId,
            ProjectId = ProjectId,
            UserId = UserId,
            Title = keyword,
            TargetKeyword = keyword,
            SerpKeyword = export.Keyword,
            TargetLocation = "United States",
            ContentHtml = "<h1>Widget repair</h1>",
            AnalysisRunId = RunId,
            SiteProfileId = SiteProfileId,
            KeywordBundleJson = ContentWriterKeywordBundleSerializer.Serialize(export),
            KeywordBundleCapturedAt = DateTimeOffset.UtcNow,
        };
    }

    internal static ContentWriterHandoffService CreateHandoffService(
        ContentWriterSerpExport? export = null,
        ContentWriterSiteBundle? siteBundle = null) =>
        new(
            new FakeAnalysisRunRepository(export ?? CompletedExport()),
            new FakeSiteAnalyzer2SiteProfileRepository(siteBundle));

    internal sealed class FakeSiteAnalyzer2SiteProfileRepository(ContentWriterSiteBundle? bundle = null)
        : ISiteAnalyzer2SiteProfileRepository
    {
        private readonly ContentWriterSiteBundle _bundle = bundle ?? MinimalSiteBundle();

        public Task<Result<SiteAnalyzer2SiteProfileExport>> GetByIdAsync(Guid siteProfileId, CancellationToken ct = default) =>
            Task.FromResult(Result<SiteAnalyzer2SiteProfileExport>.NotFound("not found"));

        public Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleAsync(
            Guid siteProfileId, CancellationToken ct = default) =>
            siteProfileId == _bundle.SiteProfileId
                ? Task.FromResult(Result<ContentWriterSiteBundle>.Success(_bundle))
                : Task.FromResult(Result<ContentWriterSiteBundle>.NotFound("not found"));
    }

    internal sealed class FakeAnalysisRunRepository(ContentWriterSerpExport? export = null) : IAnalysisRunRepository
    {
        public Task<Result<IReadOnlyList<AnalysisRunSummary>>> ListByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            export is not null && projectId == export.ProjectId
                ? Task.FromResult(Result<IReadOnlyList<AnalysisRunSummary>>.Success(
                [
                    new AnalysisRunSummary
                    {
                        Id = export.RunId,
                        ProjectId = export.ProjectId,
                        Keyword = export.Keyword,
                        Status = export.Status,
                        TargetSiteUrl = export.TargetSiteUrl,
                        SerpSeResultsCount = export.SerpSeResultsCount,
                        OrganicResultCount = export.Serp.Count(i =>
                            string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase)),
                        ContentWritingReady = export.Serp.Any(i =>
                            string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase)),
                    },
                ]))
                : Task.FromResult(Result<IReadOnlyList<AnalysisRunSummary>>.Success([]));

        public Task<Result<ContentWriterSerpExport>> GetContentWriterExportAsync(Guid runId, CancellationToken ct = default) =>
            export is not null && runId == export.RunId
                ? Task.FromResult(Result<ContentWriterSerpExport>.Success(export))
                : Task.FromResult(Result<ContentWriterSerpExport>.NotFound("not found"));
    }
}
