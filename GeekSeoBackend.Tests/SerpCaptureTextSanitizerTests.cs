using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpCaptureTextSanitizerTests
{
    [Fact]
    public void Sanitize_rejects_ai_overview_unavailable_capture()
    {
        const string junk =
            "artificial intelligence Market Intelligence & Analytics is is not available for this search " +
            "Can't generate an AI overview right now. Try again later. AI Overview (function(){window.sn={" +
            "_setImageSrc:function(b,c){var a=document.getElementById(`${b}`);";

        Assert.Null(SerpCaptureTextSanitizer.Sanitize(junk));
        Assert.False(SerpCaptureTextSanitizer.IsUsable(junk));
    }

    [Fact]
    public void Sanitize_keeps_plain_featured_snippet_text()
    {
        const string snippet = "Widget repair typically costs $50 to $200 depending on damage and parts.";

        Assert.Equal(snippet, SerpCaptureTextSanitizer.Sanitize(snippet));
    }
}

public sealed class WritingResearchBenchmarkResolverTests
{
    [Fact]
    public void ToSerpFeatures_does_not_mark_ai_overview_text_as_featured_snippet()
    {
        var research = new WritingResearchContext
        {
            AnalysisRunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://example.com",
            DerivedKeyword = "market intelligence",
            SerpKeyword = "market intelligence",
            SearchLocation = "United States",
            BusinessContext = string.Empty,
            DataQuality = "live",
            IntentPrimary = "informational",
            IntentJustification = "test",
            Paf = new WritingResearchPaf
            {
                Type = "ai_overview",
                Format = "mixed",
                Text = "Can't generate an AI overview right now.",
                BeatStrategy = string.Empty,
            },
            MustBeatPaf = true,
            DirectAnswerInstruction = "Lead with a concise definition.",
            Benchmarks = new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = 1200,
                MedianTitleLengthTop10 = 55,
                MedianH2CountTop5 = 4,
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

        var features = WritingResearchBenchmarkResolver.ToSerpFeatures(research);

        Assert.True(features.HasAiOverview);
        Assert.False(features.HasFeaturedSnippet);
    }
}
