using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ResearchDraftPipelineTests
{
    [Fact]
    public void BuildResearchDraftSystemPrompt_includes_identity_line_for_geekatyourspot()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch();
        var prompt = ArticlePromptBuilder.BuildResearchDraftSystemPrompt(research);

        Assert.Contains("Geek at Your Spot", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IT consultancy", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ai customer journey", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Original prose only", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildResearchDraftUserPrompt_uses_structural_patterns_not_raw_competitor_titles()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch() with
        {
            Competitors =
            [
                new WritingResearchCompetitor
                {
                    Url = "https://competitor.com/guide",
                    Position = 1,
                    H1 = "How to Build an AI Customer Journey Map",
                    EstimatedWordCount = 1200,
                    Headings = [],
                },
            ],
        };

        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = research.DerivedKeyword,
        });

        Assert.Contains("Structural subtopic patterns", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("competitor.com", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prospecting_keyword_family_selects_apollo_and_clay()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch() with
        {
            DerivedKeyword = "AI for Prospecting & Lead Intelligence",
            SerpKeyword = "AI for Prospecting & Lead Intelligence",
        };

        var pack = BusinessVoicePackBuilder.Build(research);

        Assert.Equal("prospecting", pack.KeywordFamilyId);
        Assert.Contains(pack.SuggestedToolExamples, t => t.Contains("Apollo", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.SuggestedToolExamples, t => t.Contains("Clay", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void General_keyword_family_keeps_shopify_defaults_for_ecommerce()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch() with
        {
            DerivedKeyword = "Shopify automation for small business",
            SerpKeyword = "Shopify automation for small business",
        };

        var pack = BusinessVoicePackBuilder.Build(research);

        Assert.Equal("general", pack.KeywordFamilyId);
        Assert.Contains(pack.SuggestedToolExamples, t => t.Contains("Shopify", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pack.SuggestedToolExamples, t => t.Contains("HubSpot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VoicePackDiagnostics_enabled_for_geekatyourspot_profile()
    {
        var diagnostics = ResearchDraftIdentityPrompt.Diagnose(BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch());

        Assert.True(diagnostics.VoicePackEnabled);
        Assert.True(diagnostics.HasSiteName);
        Assert.True(diagnostics.UserPromptIncludesVoicePack);
    }

    [Fact]
    public void VoicePackDiagnostics_disabled_without_site_focus_or_context()
    {
        var research = new WritingResearchContext
        {
            AnalysisRunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://example.com/",
            DerivedKeyword = "generic topic",
            SearchLocation = "United States",
            IntentPrimary = "informational",
            IntentJustification = "guide",
            Paf = new WritingResearchPaf { Type = "none", Format = "text" },
            DirectAnswerInstruction = "Define the topic.",
            Benchmarks = new WritingResearchBenchmarks { MedianWordCountTop5 = 1000, DominantContentFormat = "guide" },
        };

        var diagnostics = ResearchDraftIdentityPrompt.Diagnose(research);

        Assert.False(diagnostics.VoicePackEnabled);
    }

    [Fact]
    public void DraftPlagiarismRules_fails_verbatim_serp_title_heading()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch() with
        {
            Organic =
            [
                new WritingResearchOrganic
                {
                    Position = 1,
                    Url = "https://competitor.com/post",
                    Title = "AI Customer Journey Mapping Guide",
                    Snippet = "A long snippet about mapping journeys for teams.",
                    Domain = "competitor.com",
                    ContentType = "guide",
                },
            ],
        };

        var html = "<h1>Guide</h1><h3>AI Customer Journey Mapping Guide</h3><p>Body.</p>";
        var report = DraftPlagiarismRules.Evaluate(html, research);

        Assert.False(report.Passed);
        Assert.Contains(report.Failures, f => f.RuleId == "verbatim_title");
    }

    [Fact]
    public void Resolve_enforces_minimum_pillar_word_count()
    {
        Assert.Equal(1600, ResearchDraftWordTarget.Resolve(0, 400));
        Assert.Equal(1600, ResearchDraftWordTarget.Resolve(800, 1200));
        Assert.Equal(2000, ResearchDraftWordTarget.Resolve(2000, 1200));
    }

    [Fact]
    public void BuildResearchDraftUserPrompt_includes_length_instructions()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch() with
        {
            Benchmarks = new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = 500,
                DominantContentFormat = "guide",
            },
        };

        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = research.DerivedKeyword,
            TargetWordCount = 500,
        });

        Assert.Contains("Minimum article length: 1600 words", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("400 words each", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DraftPlagiarismRules_passes_original_headings()
    {
        var research = BusinessVoicePackTestsFixtures.GeekAtYourSpotResearch();
        var html = """
            <h1>AI customer journey</h1>
            <h2>Why ai customer journey matters now</h2>
            <p>Original implementation guidance.</p>
            """;

        Assert.True(DraftPlagiarismRules.PassesAllRules(html, research));
    }
}

internal static class BusinessVoicePackTestsFixtures
{
    public static WritingResearchContext GeekAtYourSpotResearch() => new()
    {
        AnalysisRunId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        SourceUrl = "https://www.geekatyourspot.com/",
        DerivedKeyword = "ai customer journey",
        SerpKeyword = "ai customer journey",
        SearchLocation = "Delray Beach, FL",
        BusinessContext =
            "Geek at Your Spot Technology consultancy in Delray Beach, Florida specializing in AI, Process Automation, AI Chatbots, Data Analytics, React, Node.js, Postgres for SMBs in Broward, Palm Beach and Miami-Dade Counties.",
        SiteFocus = new SiteWritingFocus
        {
            SiteName = "Geek at Your Spot",
            SiteUrl = "https://www.geekatyourspot.com/",
            BusinessSummary =
                "Technology consultancy integrating AI, chatbots, automation, and custom React/Node apps for South Florida SMBs.",
            PrimaryNiche = "AI implementation for SMBs",
            ServiceAreaDescription = "Broward County, Palm Beach County, Miami-Dade County",
            GeoAnchorNodes = ["Delray Beach, FL, US"],
            NicheTags = ["AI Chatbots", "Process Automation", "React", "Postgres"],
            WritingInstructions =
                "Positioning: you sell AI implementation for SMBs in South Florida. Show named tools and deployment scenarios.",
        },
        IntentPrimary = "informational",
        IntentJustification = "guide",
        DirectAnswerInstruction = "Lead with a concise definition.",
        Paf = new WritingResearchPaf { Type = "none", Format = "text" },
        Benchmarks = new WritingResearchBenchmarks
        {
            MedianWordCountTop5 = 1800,
            DominantContentFormat = "guide",
        },
        RecommendedTerms = [],
        SectionHints = [],
        ClosingFaqs = [],
        PeopleAlsoAsk = [],
        RelatedSearches = [],
        Organic = [],
        Competitors = [],
        SourceHeadings = [],
    };
}
