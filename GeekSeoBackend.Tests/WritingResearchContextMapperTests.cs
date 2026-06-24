using GeekSeo.Application.Mapping;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class WritingResearchContextMapperTests
{
    [Fact]
    public void FromEntity_MapsParentAndChildCollections()
    {
        var researchId = Guid.NewGuid();
        var row = new SeoUrlResearch
        {
            Id = researchId,
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://example.com/blog/post",
            DerivedKeyword = "example keyword",
            SearchLocation = "United States",
            Status = "completed",
            IntentPrimary = "informational",
            IntentJustification = "how-to guide",
            PafType = "paragraph",
            PafFormat = "text",
            DirectAnswerInstruction = "Answer in first paragraph.",
            DominantContentFormat = "guide",
            MedianWordCountTop5 = 1500,
            RecommendedTerms =
            [
                new SeoUrlResearchTerm { UrlResearchId = researchId, Term = "widget", DisplayOrder = 1 },
            ],
            ClosingFaqs =
            [
                new SeoUrlResearchClosingFaq
                {
                    UrlResearchId = researchId,
                    Question = "What is a widget?",
                    Source = "paa",
                    DisplayOrder = 1,
                },
            ],
            SectionHints =
            [
                new SeoUrlResearchSectionHint
                {
                    UrlResearchId = researchId,
                    DisplayOrder = 1,
                    SuggestedH2 = "Business Objectives",
                    SubtopicsFromSerp = ["ROI", "timeline"],
                },
            ],
        };

        var context = WritingResearchContextMapper.FromEntity(row);

        Assert.Equal(researchId, context.AnalysisRunId);
        Assert.Equal("example keyword", context.DerivedKeyword);
        Assert.Single(context.RecommendedTerms);
        Assert.Equal("widget", context.RecommendedTerms[0].Term);
        Assert.Single(context.ClosingFaqs);
        Assert.Equal("What is a widget?", context.ClosingFaqs[0].Question);
        Assert.Equal("Business Objectives", context.SectionHints[0].SuggestedH2);
        Assert.Equal(1500, context.Benchmarks.MedianWordCountTop5);
    }
}
