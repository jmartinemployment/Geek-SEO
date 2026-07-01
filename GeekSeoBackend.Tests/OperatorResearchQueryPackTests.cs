using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class OperatorResearchQueryPackTests
{
    [Fact]
    public void Build_includes_expected_buckets_and_junk_exclusion()
    {
        var queries = OperatorResearchQueryPack.Build(new OperatorResearchQueryOptions
        {
            Keyword = "AI customer journey",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
            LocalCity = "San Francisco",
        });

        Assert.Contains(queries, q => q.Bucket == "citations_wikipedia" && q.Query.Contains("site:en.wikipedia.org"));
        Assert.Contains(queries, q => q.Bucket == "paa_supplement" && q.Query.Contains("(how OR why"));
        Assert.Contains(queries, q => q.Bucket == "featured_snippet" && q.Query.Contains("what is"));
        Assert.Contains(queries, q => q.Bucket == "news" && q.Query.Contains("after:"));
        Assert.Contains(queries, q => q.Bucket == "local_angle" && q.Query.Contains("San Francisco"));
        Assert.Contains(queries, q => q.Bucket == "own_site" && q.Query.Contains("site:geekatyourspot.com"));
        Assert.Contains(queries, q => q.Bucket == "citations_wikipedia" && q.Query.Contains("-reddit"));
    }
}

public sealed class OperatorResearchEnricherTests
{
    [Fact]
    public async Task EnrichContextAsync_merges_authoritative_citations_and_filters_organic()
    {
        var serp = new FakeOperatorSerpProvider();
        serp.Responses["wikipedia"] = new SerpResult
        {
            Keyword = "x",
            Location = "United States",
            OrganicResults =
            [
                new SerpOrganicResult
                {
                    Position = 1,
                    Url = "https://en.wikipedia.org/wiki/Customer_journey",
                    Title = "Customer journey",
                    Domain = "wikipedia.org",
                    Snippet = "Overview",
                },
            ],
            Features = new SerpFeatures(),
            FetchedAt = DateTimeOffset.UtcNow,
        };

        var enricher = new OperatorResearchEnricher(serp);
        var context = new WritingResearchContext
        {
            AnalysisRunId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SourceUrl = "https://www.geekatyourspot.com/",
            DerivedKeyword = "AI customer journey",
            SerpKeyword = "AI customer journey",
            SearchLocation = "United States",
            IntentPrimary = "informational",
            IntentJustification = "test",
            Paf = new WritingResearchPaf { Type = "none", Format = "paragraph" },
            DirectAnswerInstruction = "test",
            Benchmarks = new WritingResearchBenchmarks { DominantContentFormat = "guide" },
            CitationCandidates =
            [
                new WritingResearchCitationCandidate
                {
                    Url = "https://competitor.com/guide",
                    Title = "Bad",
                    Domain = "competitor.com",
                    Source = "organic",
                },
            ],
        };

        var templates = OperatorResearchQueryPack.Build(new OperatorResearchQueryOptions
        {
            Keyword = context.DerivedKeyword,
            TargetSiteUrl = context.SourceUrl,
        });
        foreach (var template in templates)
        {
            if (template.Bucket == "citations_wikipedia")
                serp.Responses[template.Query] = serp.Responses["wikipedia"];
            else
                serp.Responses[template.Query] = EmptySerp();
        }

        var enriched = await enricher.EnrichContextAsync(context);

        Assert.Contains(enriched.CitationCandidates, c => c.Url.Contains("wikipedia.org", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(enriched.CitationCandidates, c => c.Url.Contains("competitor.com", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(enriched.OperatorQueries);
    }

    private static SerpResult EmptySerp() => new()
    {
        Keyword = "x",
        Location = "United States",
        OrganicResults = [],
        Features = new SerpFeatures(),
        FetchedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeOperatorSerpProvider : GeekSeo.Application.Interfaces.Seo.ISerpProvider
    {
        public string ProviderName => "serpapi";
        public Dictionary<string, SerpResult> Responses { get; } = new(StringComparer.Ordinal);

        public Task<GeekSeo.Application.Results.Result<SerpResult>> GetSerpResultsAsync(
            SerpRequest request,
            CancellationToken ct = default)
        {
            if (Responses.TryGetValue(request.Keyword, out var result))
                return Task.FromResult(GeekSeo.Application.Results.Result<SerpResult>.Success(result));

            return Task.FromResult(GeekSeo.Application.Results.Result<SerpResult>.Success(EmptySerp()));
        }
    }
}
