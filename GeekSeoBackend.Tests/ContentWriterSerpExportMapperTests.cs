using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentWriterSerpExportMapperTests
{
    private static readonly Guid RunId = AnalysisRunTestData.RunId;
    private static readonly Guid ProjectId = AnalysisRunTestData.ProjectId;
    private static readonly Guid UserId = AnalysisRunTestData.UserId;

    [Fact]
    public void ToWritingResearchContext_maps_organic_competitors_pasf_and_paa()
    {
        var export = new ContentWriterSerpExport
        {
            RunId = RunId,
            ProjectId = ProjectId,
            Keyword = "widget repair near me",
            TargetSiteUrl = "https://example.com",
            Status = "completed",
            SerpSeResultsCount = 1_250_000,
            Serp =
            [
                new ContentWriterSerpItem
                {
                    Position = 1,
                    Type = "organic",
                    Title = "Widget Repair Services",
                    Url = "https://competitor-a.com/widget-repair",
                    Domain = "competitor-a.com",
                    Snippet = "Professional widget repair for homes and businesses.",
                },
                new ContentWriterSerpItem
                {
                    Position = 2,
                    Type = "organic",
                    Title = "Local Widget Experts",
                    Url = "https://competitor-b.com/services",
                    Domain = "competitor-b.com",
                    Snippet = "Same-day widget repair with warranty.",
                },
                new ContentWriterSerpItem
                {
                    Position = 3,
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
                    Position = 4,
                    Type = "people_also_ask",
                    RelatedQuestions = ["What is widget repair?"],
                    Snippet = "Widget repair restores broken components.",
                },
                new ContentWriterSerpItem
                {
                    Position = 5,
                    Type = "ai_overview",
                    Snippet = "Widget repair typically costs $50–$200 depending on damage.",
                },
            ],
        };

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(
            export,
            UserId,
            "United States");

        Assert.Equal(RunId, context.AnalysisRunId);
        Assert.Equal(ProjectId, context.ProjectId);
        Assert.Equal(UserId, context.UserId);
        Assert.Equal("widget repair near me", context.DerivedKeyword);
        Assert.Equal(2, context.Organic.Count);
        Assert.Equal(2, context.Competitors.Count);
        Assert.Equal(3, context.RelatedSearches.Count);
        Assert.Equal("widget repair cost", context.RelatedSearches[0].SearchText);
        Assert.Contains(context.PeopleAlsoAsk, p => p.Question == "What is widget repair?");
        Assert.Equal("Widget repair restores broken components.", context.PeopleAlsoAsk[0].SerpAnswerPreview);
        Assert.Contains(context.RecommendedTerms, t => t.Term == "widget repair cost");
        Assert.Contains(context.ClosingFaqs, f => f.Source == "paa");
        Assert.NotEmpty(context.SectionHints);
        Assert.Equal("ai_overview", context.Paf.Type);
        Assert.Contains("Lead with a concise definition", context.DirectAnswerInstruction, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Match or beat", context.DirectAnswerInstruction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToWritingResearchContext_uses_article_keyword_when_provided()
    {
        var export = AnalysisRunTestData.CompletedExport() with { Keyword = "widget repair near me" };

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(
            export,
            UserId,
            "United States",
            "emergency widget repair");

        Assert.Equal("emergency widget repair", context.DerivedKeyword);
        Assert.Equal("widget repair near me", context.SerpKeyword);
        Assert.Contains("emergency widget repair", context.DirectAnswerInstruction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToWritingResearchContext_maps_market_research_pasf()
    {
        var export = AnalysisRunTestData.MarketResearchPasfExport();

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(export, UserId);

        Assert.Equal(8, context.RelatedSearches.Count);
        Assert.Equal("ai in marketing analytics course", context.RelatedSearches[0].SearchText);
        Assert.Equal("ai market research report", context.RelatedSearches[7].SearchText);
        Assert.Contains(context.RecommendedTerms, t => t.Term == "free ai tools for market research");
        Assert.Contains(context.ClosingFaqs, f => f.Source == "pasf");
        Assert.Empty(context.PeopleAlsoAsk);
    }

    [Fact]
    public void ToWritingResearchContext_collects_paa_from_item_related_questions()
    {
        var export = new ContentWriterSerpExport
        {
            RunId = RunId,
            ProjectId = ProjectId,
            Keyword = "zapier quickbooks integration",
            Serp =
            [
                new ContentWriterSerpItem
                {
                    Position = 1,
                    Type = "people_also_ask",
                    RelatedQuestions = ["How much does Zapier QuickBooks integration cost?"],
                    Snippet = "Pricing depends on Zapier plan tier.",
                },
            ],
        };

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(export, UserId);

        Assert.Single(context.PeopleAlsoAsk);
        Assert.Equal("How much does Zapier QuickBooks integration cost?", context.PeopleAlsoAsk[0].Question);
        Assert.NotEmpty(context.ClosingFaqs);
    }

    [Fact]
    public void ToWritingResearchContext_maps_citation_candidates_and_faq_schema()
    {
        var export = AnalysisRunTestData.CompletedExport();

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(export, UserId);

        Assert.Single(context.Competitors);
        Assert.True(context.Competitors[0].HasFaqSchema);
        Assert.Contains("FAQPage", context.Competitors[0].SchemaTypes);
        Assert.Equal(2, context.Competitors[0].Headings.Count);
        Assert.NotEmpty(context.CitationCandidates);
        Assert.Contains(context.CitationCandidates, c => c.Source == "organic");
    }
}
