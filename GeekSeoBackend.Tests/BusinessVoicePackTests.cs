using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class BusinessVoicePackTests
{
    [Fact]
    public void Build_detects_geekatyourspot_capabilities_and_geo()
    {
        var research = GeekAtYourSpotResearch();

        var pack = BusinessVoicePackBuilder.Build(research);

        Assert.True(pack.Enabled);
        Assert.True(pack.IsImplementationConsultancy);
        Assert.Contains(pack.DeclaredCapabilities, c => c.Contains("chatbot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Broward", pack.GeoLabel, StringComparison.OrdinalIgnoreCase);
        Assert.True(pack.RequiresTraditionalVsAiContrast);
        Assert.True(pack.RequiresCapabilityBridge);
        Assert.Contains("Shopify", pack.SuggestedToolExamples);
        Assert.Contains("free strategy call", pack.CtaParagraphHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildResearchDraftUserPrompt_includes_business_voice_pack()
    {
        var research = GeekAtYourSpotResearch();
        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = "Create a map of my customer journey",
            TargetWordCount = 1800,
        });

        Assert.Contains("Business voice pack", prompt);
        Assert.Contains("old-way vs. AI-way", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("each of the four methodology sections", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("free strategy call", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pick from:", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCtaBeforeFaq_inserts_cta_before_faq_section()
    {
        var pack = BusinessVoicePackBuilder.Build(GeekAtYourSpotResearch());
        var html = "<h1>Title</h1><p>Body</p><h2>Frequently Asked Questions</h2><h3>Q?</h3><p>A.</p>";

        var enriched = BusinessVoiceDraftEnricher.EnsureCtaBeforeFaq(html, pack);

        Assert.Contains("free strategy call", enriched, StringComparison.OrdinalIgnoreCase);
        Assert.True(enriched.IndexOf("free strategy call", StringComparison.OrdinalIgnoreCase)
            < enriched.IndexOf("Frequently Asked Questions", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_passes_article_with_examples_contrast_and_capability_bridge()
    {
        var pack = BusinessVoicePackBuilder.Build(GeekAtYourSpotResearch());
        var html = """
            <h1>AI customer journey</h1>
            <p>Geek at Your Spot helps South Florida SMBs in Broward and Palm Beach map live customer data from call to booking follow-up.</p>
            <h2>Why ai customer journey matters now</h2>
            <p>Using HubSpot stages and Shopify order tags, teams see where leads stall.</p>
            <h2>Data readiness before you implement ai customer journey</h2>
            <p>The old way relied on whiteboard personas built on a hunch. The AI way clusters thousands of live support transcripts in real time.</p>
            <h2>Choosing the right AI technologies for ai customer journey</h2>
            <p>We integrate Postgres analytics with React dashboards and deploy AI chatbots on your stack.</p>
            <h2>Implementation strategy and rollout for ai customer journey</h2>
            <p>Our team pilots one journey stage at a time.</p>
            <p><strong>Want to see what ai customer journey looks like for your specific tech stack?</strong> Book a free strategy call.</p>
            <h2>Frequently Asked Questions</h2>
            <h3>What is an AI customer journey map?</h3><p>Answer.</p>
            """;

        Assert.True(BusinessVoiceValidator.PassesAllGates(html, pack));
    }

    private static WritingResearchContext GeekAtYourSpotResearch() => new()
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
