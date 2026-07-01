using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpQuestionFilterTests
{
    [Theory]
    [InlineData("Where can I find an AI customer journey PDF?")]
    [InlineData("AI customer journey template download")]
    [InlineData("AI journey map generator")]
    [InlineData("best ai for market research reddit")]
    [InlineData("ai in marketing analytics course")]
    public void IsBlocked_rejects_resource_seeker_and_off_intent_phrases(string phrase)
    {
        Assert.True(SerpQuestionFilter.IsBlocked(phrase));
    }

    [Theory]
    [InlineData("How much does widget repair cost?")]
    [InlineData("What is an AI customer journey map?")]
    [InlineData("How can a small business use AI in the customer journey?")]
    public void IsBlocked_allows_buyer_questions(string phrase)
    {
        Assert.False(SerpQuestionFilter.IsBlocked(phrase));
    }

    [Fact]
    public void BuildClosingFaqQuestions_skips_blocked_paa()
    {
        var questions = ContentWritingRules.BuildClosingFaqQuestions(
            "ai customer journey",
            [
                "Where can I find an AI customer journey PDF?",
                "What is an AI customer journey map?",
            ],
            null);

        Assert.Equal(5, questions.Count);
        Assert.DoesNotContain(questions, q => q.Contains("PDF", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(questions, q => q.Contains("AI customer journey map", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToWritingResearchContext_filters_pdf_paa_from_closing_faqs()
    {
        var export = new ContentWriterSerpExport
        {
            RunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "ai customer journey",
            Serp =
            [
                new ContentWriterSerpItem
                {
                    Position = 1,
                    Type = "people_also_ask",
                    RelatedQuestions =
                    [
                        "Where can I find an AI customer journey PDF?",
                        "What is an AI customer journey map?",
                    ],
                },
            ],
        };

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(export, Guid.NewGuid());

        Assert.DoesNotContain(context.ClosingFaqs, f => f.Question.Contains("PDF", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(context.ClosingFaqs, f => f.Question.Contains("AI customer journey map", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildResearchDraftUserPrompt_orders_filtered_closing_faqs_not_raw_paa()
    {
        var research = new WritingResearchContext
        {
            AnalysisRunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://geekatyourspot.com",
            DerivedKeyword = "ai customer journey",
            SerpKeyword = "ai customer journey",
            SearchLocation = "Delray Beach, FL",
            BusinessContext = "Local AI consultancy serving South Florida SMBs.",
            DataQuality = "partial",
            IntentPrimary = "informational",
            IntentJustification = "guide",
            DirectAnswerInstruction = "Lead with a concise definition.",
            Paf = new WritingResearchPaf { Type = "none", Format = "text" },
            Benchmarks = new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = 1600,
                DominantContentFormat = "guide",
            },
            RecommendedTerms = [],
            SectionHints = [],
            ClosingFaqs =
            [
                new WritingResearchClosingFaq
                {
                    Question = "What is an AI customer journey map?",
                    Source = "paa",
                    DisplayOrder = 1,
                },
            ],
            PeopleAlsoAsk =
            [
                new WritingResearchPaa { Question = "Where can I find an AI customer journey PDF?", DisplayOrder = 1 },
            ],
            RelatedSearches = [],
            Organic = [],
            Competitors = [],
            SourceHeadings = [],
        };

        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = "Create a map of my customer journey",
            TargetWordCount = 1600,
        });

        Assert.Contains("Use these questions in order:", prompt);
        Assert.Contains("What is an AI customer journey map?", prompt);
        Assert.DoesNotContain("AI customer journey PDF", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("prioritize these", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Business voice pack", prompt);
        Assert.Contains("exactly 4 body sections", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
