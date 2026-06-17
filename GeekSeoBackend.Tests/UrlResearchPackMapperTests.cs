using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Tests;

public sealed class UrlResearchPackMapperTests
{
    [Theory]
    [InlineData("live", "full")]
    [InlineData("partial", "partial")]
    [InlineData("unavailable", "weak")]
    public void ToFullWrite_maps_data_quality(string packQuality, string expectedDbQuality)
    {
        var write = UrlResearchPackMapper.ToFullWrite(SamplePack(packQuality));

        Assert.Equal(expectedDbQuality, write.DataQuality);
    }

    [Fact]
    public void ToFullWrite_maps_core_fields_and_child_collections()
    {
        var pack = SamplePack("live") with
        {
            Meta = SamplePack("live").Meta with
            {
                Keyword = "widget repair",
                Location = "Austin, TX",
                BusinessContext = "Local repair shop.",
                Notes = ["SERP partial for PAA"],
            },
            RecommendedTerms = ["widget", "repair"],
            ClosingFaqQuestions =
            [
                new SerpResearchClosingFaqItem { Question = "How much?", Source = "paa" },
            ],
            MethodologyHints =
            [
                new SerpResearchMethodologyHint
                {
                    Movement = 1,
                    Label = "objectives",
                    SuggestedH2 = "Business Objectives",
                    SubtopicsFromSerp = ["ROI"],
                },
            ],
            SourceHeadings =
            [
                new SerpResearchHeading { Level = 2, Text = "Our services" },
            ],
        };

        var write = UrlResearchPackMapper.ToFullWrite(pack);

        Assert.Equal("widget repair", write.DerivedKeyword);
        Assert.Equal("Austin, TX", write.SearchLocation);
        Assert.Equal("Local repair shop.", write.BusinessContext);
        Assert.Equal("SERP partial for PAA", write.DataQualityNotes);
        Assert.Equal("informational", write.IntentPrimary);
        Assert.Equal(1500, write.MedianWordCountTop5);
        Assert.Equal(6, write.MedianH2CountTop5);
        Assert.Equal(2, write.RecommendedTerms.Count);
        Assert.Equal("widget", write.RecommendedTerms[0].Term);
        Assert.Single(write.ClosingFaqs);
        Assert.Equal("How much?", write.ClosingFaqs[0].Question);
        Assert.Single(write.SectionHints);
        Assert.Equal("Business Objectives", write.SectionHints[0].SuggestedH2);
        Assert.Single(write.SourceHeadings);
        Assert.Equal("Our services", write.SourceHeadings[0].Text);
        Assert.Equal(2, write.Competitors.Count);
        Assert.Equal("https://competitor.example/a", write.Competitors[0].Url);
    }

    private static SerpResearchPack SamplePack(string dataQuality) => new()
    {
        Meta = new SerpResearchPackMeta
        {
            SourceUrl = "https://example.com/page",
            Keyword = "example keyword",
            Location = "United States",
            ResearchedAt = "2026-06-17T12:00:00Z",
            DataQuality = dataQuality,
        },
        Intent = new SerpResearchIntent
        {
            Primary = "informational",
            Justification = "how-to intent",
        },
        Paf = new SerpResearchPaf
        {
            Type = "paragraph",
            Format = "text",
            Text = "Answer here.",
            SourceUrl = "https://example.com/snippet",
            BeatStrategy = "be more specific",
        },
        Paa = [],
        Pasf = ["related one"],
        SerpFeatures = ["people_also_ask"],
        Organic =
        [
            new SerpResearchOrganicItem
            {
                Position = 1,
                Url = "https://competitor.example/a",
                Domain = "competitor.example",
                Title = "Example",
                Snippet = "Snippet",
                ContentType = "article",
            },
        ],
        CompetitorOutlines =
        [
            new SerpResearchCompetitorOutline
            {
                Url = "https://competitor.example/a",
                Position = 1,
                H1 = "Title",
                EstimatedWordCount = 1200,
                Headings =
                [
                    new SerpResearchHeading { Level = 2, Text = "One" },
                    new SerpResearchHeading { Level = 2, Text = "Two" },
                ],
            },
            new SerpResearchCompetitorOutline
            {
                Url = "https://competitor.example/b",
                Position = 2,
                H1 = "Other",
                EstimatedWordCount = 900,
                Headings =
                [
                    new SerpResearchHeading { Level = 2, Text = "A" },
                    new SerpResearchHeading { Level = 2, Text = "B" },
                    new SerpResearchHeading { Level = 2, Text = "C" },
                    new SerpResearchHeading { Level = 2, Text = "D" },
                    new SerpResearchHeading { Level = 2, Text = "E" },
                    new SerpResearchHeading { Level = 2, Text = "F" },
                ],
            },
        ],
        SourceHeadings = [],
        Benchmarks = new SerpResearchBenchmarks
        {
            MedianWordCountTop5 = 1500,
            MedianTitleLengthTop10 = 55,
            DominantContentFormat = "guide",
        },
        RecommendedTerms = [],
        ClosingFaqQuestions = [],
        DirectAnswerBlock = new SerpResearchDirectAnswerBlock
        {
            Instruction = "Lead with a direct answer.",
            MustBeatPaf = true,
        },
        MethodologyHints = [],
    };
}
