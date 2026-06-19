using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerPackValidatorTests
{
    private static readonly Guid SiteId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void ValidateCompletePack_rejects_without_site_research_link()
    {
        var research = MinimalComplete();
        research.SiteResearchId = null;

        var result = SiteAnalyzerPackValidator.ValidateCompletePack(research);
        Assert.False(result.Passed);
    }

    [Fact]
    public void ValidateCompletePack_rejects_partial_data_quality()
    {
        var research = MinimalComplete();
        research.DataQuality = "partial";

        var result = SiteAnalyzerPackValidator.ValidateCompletePack(research);
        Assert.False(result.Passed);
    }

    [Fact]
    public void ValidateCompletePack_passes_full_pack()
    {
        var research = MinimalComplete();
        var result = SiteAnalyzerPackValidator.ValidateCompletePack(research);
        Assert.True(result.Passed);
    }

    internal static SeoUrlResearch MinimalComplete()
    {
        return new SeoUrlResearch
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://example.com",
            DerivedKeyword = "widget repair",
            Status = "completed",
            DataQuality = "full",
            SiteResearchId = SiteId,
            BusinessContext = "Local widget repair shop.",
            PafType = "none",
            IntentPrimary = "informational",
            IntentJustification = "test",
            PafFormat = "",
            DirectAnswerInstruction = "Answer directly.",
            DominantContentFormat = "guide",
            OrganicResults = [new SeoUrlResearchOrganic { Id = Guid.NewGuid(), UrlResearchId = Guid.NewGuid(), Position = 1, Url = "https://a.com", Domain = "a.com", Title = "A", Snippet = "s", ContentType = "guide" }],
            PeopleAlsoAsk = [new SeoUrlResearchPaa { Id = Guid.NewGuid(), UrlResearchId = Guid.NewGuid(), Question = "Q?" }],
            RelatedSearches = [new SeoUrlResearchPasf { Id = Guid.NewGuid(), UrlResearchId = Guid.NewGuid(), SearchText = "related" }],
            Competitors =
            [
                Competitor(1), Competitor(2), Competitor(3),
            ],
            RecommendedTerms = Enumerable.Range(1, 8).Select(i => new SeoUrlResearchTerm
            {
                Id = Guid.NewGuid(),
                UrlResearchId = Guid.NewGuid(),
                Term = $"term{i}",
            }).ToList(),
            SectionHints = Enumerable.Range(1, 4).Select(i => new SeoUrlResearchSectionHint
            {
                Id = Guid.NewGuid(),
                UrlResearchId = Guid.NewGuid(),
                DisplayOrder = i,
                Label = $"Section {i}",
            }).ToList(),
            ClosingFaqs = Enumerable.Range(1, 5).Select(i => new SeoUrlResearchClosingFaq
            {
                Id = Guid.NewGuid(),
                UrlResearchId = Guid.NewGuid(),
                Question = $"FAQ {i}?",
                Source = "suggested",
            }).ToList(),
        };
    }

    private static SeoUrlResearchCompetitor Competitor(int position) => new()
    {
        Id = Guid.NewGuid(),
        UrlResearchId = Guid.NewGuid(),
        Url = $"https://c{position}.com",
        Position = position,
        H1 = $"Heading {position}",
        Headings = [new SeoUrlResearchCompetitorHeading { Id = Guid.NewGuid(), CompetitorId = Guid.NewGuid(), Level = 2, Text = "Sub", DisplayOrder = 0 }],
    };
}
