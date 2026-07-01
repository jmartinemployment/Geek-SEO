using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ManualResearchLaneMergerTests
{
    [Fact]
    public void Merge_tags_gov_citation_from_domain_not_folder()
    {
        var export = new ContentWriterSerpExport
        {
            RunId = Guid.NewGuid(),
            Keyword = "ai customer journey",
            ManualResearchLanes =
            [
                new ContentWriterManualResearchLane
                {
                    Lane = SerpResearchLanes.Gov,
                    Label = "Government",
                    OrganicCount = 1,
                    OrganicResults =
                    [
                        new ContentWriterSerpItem
                        {
                            Position = 1,
                            Type = "organic",
                            Title = "NIST AI",
                            Url = "https://www.nist.gov/ai",
                            Domain = "nist.gov",
                            Snippet = "AI guidance",
                        },
                    ],
                },
            ],
        };

        var merged = ManualResearchLaneMerger.Merge(export);

        Assert.Contains(merged.CitationCandidates, c =>
            c.Url.Contains("nist.gov", StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.Source, "government", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Merge_drops_non_gov_url_in_gov_lane()
    {
        var export = new ContentWriterSerpExport
        {
            RunId = Guid.NewGuid(),
            Keyword = "test",
            ManualResearchLanes =
            [
                new ContentWriterManualResearchLane
                {
                    Lane = SerpResearchLanes.Gov,
                    Label = "Government",
                    OrganicCount = 1,
                    OrganicResults =
                    [
                        new ContentWriterSerpItem
                        {
                            Position = 1,
                            Type = "organic",
                            Title = "Forbes",
                            Url = "https://www.forbes.com/article",
                            Domain = "forbes.com",
                        },
                    ],
                },
            ],
        };

        var merged = ManualResearchLaneMerger.Merge(export);
        Assert.DoesNotContain(merged.CitationCandidates, c => c.Url.Contains("forbes.com"));
    }
}

public sealed class ManualResearchGateTests
{
    [Fact]
    public void ValidateManualResearchExport_requires_keyword_organics()
    {
        var export = ManualExport() with { Serp = [] };
        var result = ResearchBackedWriteGate.ValidateManualResearchExport(export);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateManualResearchExport_customer_journey_requires_gov_and_wiki()
    {
        var export = ManualExport() with
        {
            TopicSlug = "customer-journey",
            ManualResearchLanes =
            [
                GovLane(1),
            ],
        };

        var result = ResearchBackedWriteGate.ValidateManualResearchExport(export);
        Assert.False(result.IsSuccess);
        Assert.Contains("wiki", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateManualResearchExport_passes_with_keyword_gov_wiki()
    {
        var export = ManualExport() with
        {
            TopicSlug = "customer-journey",
            ManualResearchLanes = [GovLane(1), WikiLane(1)],
        };

        var result = ResearchBackedWriteGate.ValidateManualResearchExport(export);
        Assert.True(result.IsSuccess);
    }

    private static ContentWriterSerpExport ManualExport() => new()
    {
        RunId = Guid.NewGuid(),
        Keyword = "ai customer journey",
        ResearchMode = ResearchModes.Manual,
        Serp =
        [
            new ContentWriterSerpItem
            {
                Position = 1,
                Type = "organic",
                Title = "Example",
                Url = "https://example.com",
            },
        ],
    };

    private static ContentWriterManualResearchLane GovLane(int count) => new()
    {
        Lane = SerpResearchLanes.Gov,
        Label = "Gov",
        OrganicCount = count,
        OrganicResults =
        [
            new ContentWriterSerpItem
            {
                Position = 1,
                Type = "organic",
                Title = "NIST",
                Url = "https://www.nist.gov/x",
            },
        ],
    };

    private static ContentWriterManualResearchLane WikiLane(int count) => new()
    {
        Lane = SerpResearchLanes.Wiki,
        Label = "Wiki",
        OrganicCount = count,
        OrganicResults =
        [
            new ContentWriterSerpItem
            {
                Position = 1,
                Type = "organic",
                Title = "Wiki",
                Url = "https://en.wikipedia.org/wiki/Customer_journey",
            },
        ],
    };
}
