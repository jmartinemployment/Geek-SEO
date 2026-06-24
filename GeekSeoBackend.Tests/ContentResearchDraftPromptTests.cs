using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentResearchDraftPromptTests
{
    [Fact]
    public void BuildResearchDraftUserPrompt_uses_section_hints_and_closing_faqs()
    {
        var research = new WritingResearchContext
        {
            AnalysisRunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://example.com/widget-repair",
            DerivedKeyword = "widget repair",
            SerpKeyword = "widget repair",
            SearchLocation = "Austin, TX",
            BusinessContext = "Local repair shop.",
            DataQuality = "partial",
            DataQualityNotes = "PAA thin in SERP.",
            IntentPrimary = "informational",
            IntentJustification = "how-to intent",
            Paf = new WritingResearchPaf
            {
                Type = "paragraph",
                Format = "text",
                Text = "Widget repair starts with diagnosis.",
                SourceUrl = "https://competitor.example/snippet",
                BeatStrategy = "Add local pricing context.",
            },
            MustBeatPaf = true,
            DirectAnswerInstruction = "Lead with a direct answer in the first paragraph.",
            Benchmarks = new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = 1600,
                MedianTitleLengthTop10 = 55,
                MedianH2CountTop5 = 4,
                DominantContentFormat = "guide",
            },
            RecommendedTerms =
            [
                new WritingResearchTerm { Term = "widget", DisplayOrder = 1 },
                new WritingResearchTerm { Term = "repair", DisplayOrder = 2 },
            ],
            SectionHints =
            [
                new WritingResearchSectionHint
                {
                    DisplayOrder = 1,
                    SuggestedH2 = "Why widget repair matters",
                    SubtopicsFromSerp = ["cost", "timeline"],
                },
            ],
            ClosingFaqs =
            [
                new WritingResearchClosingFaq
                {
                    Question = "How much does widget repair cost?",
                    Source = "paa",
                    DisplayOrder = 1,
                },
            ],
            PeopleAlsoAsk = [],
            RelatedSearches = [],
            Organic = [],
            Competitors = [],
            SourceHeadings = [],
        };

        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = "Widget repair guide",
            TargetWordCount = 1600,
        });

        Assert.Contains("widget repair", prompt);
        Assert.Contains("Why widget repair matters", prompt);
        Assert.Contains("PAA thin in SERP.", prompt);
        Assert.Contains("Lead with a direct answer", prompt);
        Assert.Contains("Beat strategy:", prompt);
        Assert.Contains("widget, repair", prompt);
        Assert.Contains("How much does widget repair cost?", prompt);
        Assert.Contains("Frequently Asked Questions", prompt);
        Assert.DoesNotContain("Movement", prompt);
    }
}
