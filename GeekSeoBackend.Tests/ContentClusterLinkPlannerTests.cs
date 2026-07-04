using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentClusterLinkPlannerTests
{
    private const string PillarKeyword = "ai market research tools";

    [Fact]
    public void Plan_filters_course_reddit_free_and_near_duplicate_pasf()
    {
        var research = BuildMarketResearchContext();
        var result = Plan(research);

        Assert.Contains(result.FilteredOut, f =>
            f.Phrase == "ai in marketing analytics course" &&
            f.RejectReason == "resource_seeker_intent");
        Assert.Contains(result.FilteredOut, f =>
            f.Phrase == "best ai for market research reddit" &&
            f.RejectReason == "resource_seeker_intent");
        Assert.Contains(result.FilteredOut, f =>
            f.Phrase == "free ai tools for market research" &&
            f.RejectReason == "free_tier_mismatch");
        Assert.Contains(result.FilteredOut, f =>
            f.Phrase == "best free ai tools for market research" &&
            f.RejectReason == "free_tier_mismatch");
        Assert.Contains(result.FilteredOut, f =>
            f.Phrase == "ai market research tool" &&
            f.RejectReason == "near_duplicate_pillar_keyword");
    }

    [Fact]
    public void Plan_keeps_best_companies_and_report_as_spoke_candidates()
    {
        var research = BuildMarketResearchContext();
        var result = Plan(research);

        var phrases = result.SpokeCandidates.Select(c => c.Phrase).ToList();
        Assert.Contains("best ai for market analysis", phrases);
        Assert.Contains("ai market research companies", phrases);
        Assert.Contains("ai market research report", phrases);
        Assert.Equal(3, result.SpokeCandidates.Count);
    }

    [Fact]
    public void Plan_rewrites_questions_instead_of_generic_pasf_template()
    {
        var research = BuildMarketResearchContext();
        var result = Plan(research);

        var companiesSpoke = result.SpokeCandidates.Single(c => c.Phrase == "ai market research companies");
        Assert.Contains("Which companies offer", companiesSpoke.SuggestedQuestion, StringComparison.Ordinal);

        var bestSpoke = result.SpokeCandidates.Single(c => c.Phrase == "best ai for market analysis");
        Assert.Contains("are best", bestSpoke.SuggestedQuestion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("What should I know about", bestSpoke.SuggestedQuestion, StringComparison.Ordinal);

        Assert.Contains(result.FaqItems, f =>
            f.Question.Contains("are best", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.FaqItems, f =>
            f.Question.Contains("Which companies offer", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_assigns_planned_blog_paths_for_spoke_backed_faqs()
    {
        var research = BuildMarketResearchContext();
        var result = Plan(research);

        var linkedFaq = result.FaqItems.Single(f =>
            f.AnchorText == "best ai for market analysis");

        Assert.Equal("/blog/best-ai-for-market-analysis", linkedFaq.TargetPath);
        Assert.Equal(SpokeLinkStatuses.Planned, linkedFaq.LinkStatus);
    }

    [Fact]
    public void Plan_assigns_body_links_to_pillar_h2_sections()
    {
        var research = BuildMarketResearchContext();
        var result = ContentClusterLinkPlanner.Plan(new ContentClusterPlannerInput
        {
            PillarKeyword = PillarKeyword,
            Research = research,
            PillarContentHtml = """
                <h2 id="implementation">Implementation approach</h2>
                <p>Teams compare AI market research tools.</p>
                <h2>Vendor selection</h2>
                <p>Shortlist vendors carefully.</p>
                <h2>Frequently Asked Questions</h2>
                """,
        });

        Assert.Equal(2, result.BodyLinks.Count);
        Assert.All(result.BodyLinks, slot =>
        {
            Assert.False(string.IsNullOrWhiteSpace(slot.InsertAfterH2Hint));
            Assert.False(string.IsNullOrWhiteSpace(slot.AnchorText));
            Assert.StartsWith("/blog/", slot.TargetPath, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Plan_fills_faq_slots_with_templates_when_filtered_pool_is_small()
    {
        var research = BuildMarketResearchContext();
        var result = Plan(research);

        Assert.Equal(5, result.FaqItems.Count);
        Assert.Contains(result.FaqItems, f => f.Source == "suggested");
    }

    [Fact]
    public void Plan_prefers_paa_before_pasf_in_merge_order()
    {
        var research = BuildMarketResearchContext() with
        {
            PeopleAlsoAsk =
            [
                new WritingResearchPaa
                {
                    Question = "How do enterprise teams evaluate AI market research vendors?",
                    DisplayOrder = 0,
                },
            ],
        };

        var result = Plan(research);

        Assert.DoesNotContain(result.FilteredOut, f =>
            f.Phrase.Contains("evaluate AI market research vendors", StringComparison.Ordinal));
        Assert.Contains(result.FaqItems, f =>
            f.Question.Contains("evaluate AI market research vendors", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("best ai for market analysis", "Which ai for market analysis are best?")]
    [InlineData("ai market research companies", "Which companies offer ai market research companies?")]
    [InlineData("free ai tools for market research", "Are there free options for free ai tools for market research?")]
    [InlineData("How can AI help with prospecting", "How can AI help with prospecting?")]
    [InlineData("How can AI help with prospecting?", "How can AI help with prospecting?")]
    [InlineData("how to How can AI help with prospecting", "How can AI help with prospecting?")]
    public void RewriteQuestion_applies_intent_patterns(string phrase, string expected)
    {
        Assert.Equal(expected, ContentClusterLinkPlanner.RewriteQuestion(phrase, PillarKeyword));
    }

    private static ContentClusterPlanResult Plan(WritingResearchContext research) =>
        ContentClusterLinkPlanner.Plan(new ContentClusterPlannerInput
        {
            PillarKeyword = PillarKeyword,
            Research = research,
            SiteFocus = new SiteWritingFocus
            {
                SiteName = "Example",
                SiteUrl = "https://example.com",
                BusinessSummary = "Paid AI market intelligence platform for enterprise teams.",
                WritingInstructions = "Sell implementation services; no free tier.",
            },
        });

    private static WritingResearchContext BuildMarketResearchContext()
    {
        var export = AnalysisRunTestData.MarketResearchPasfExport();
        return ContentWriterSerpExportMapper.ToWritingResearchContext(
            export,
            AnalysisRunTestData.UserId,
            articleKeyword: PillarKeyword);
    }
}
