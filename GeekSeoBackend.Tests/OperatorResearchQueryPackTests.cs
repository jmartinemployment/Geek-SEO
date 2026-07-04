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
        Assert.DoesNotContain(queries, q => q.Bucket == "featured_snippet");
        Assert.Contains(queries, q => q.Bucket == "citations_wikipedia" && q.Query.Contains("-reddit"));
        Assert.DoesNotContain(queries, q => q.Bucket == "local_angle");
        Assert.DoesNotContain(queries, q => q.Bucket == "own_site");
        Assert.DoesNotContain(queries, q => q.Bucket == "contrast_traditional");
        Assert.DoesNotContain(queries, q => q.Bucket == "news");
        Assert.DoesNotContain(queries, q => q.Bucket == "featured_snippet_alt");
    }

    [Fact]
    public void Build_omits_local_angle_even_when_local_city_set()
    {
        var queries = OperatorResearchQueryPack.Build(new OperatorResearchQueryOptions
        {
            Keyword = "AI customer journey",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
            LocalCity = "San Francisco",
        });

        Assert.DoesNotContain(queries, q => q.Bucket == "local_angle");
    }

    [Fact]
    public void Build_omits_local_angle_when_no_local_city()
    {
        var queries = OperatorResearchQueryPack.Build(new OperatorResearchQueryOptions
        {
            Keyword = "AI customer journey",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
        });

        Assert.DoesNotContain(queries, q => q.Bucket == "local_angle");
    }

    [Fact]
    public void Build_omits_local_angle_for_resolved_city()
    {
        var queries = OperatorResearchQueryPack.Build(new OperatorResearchQueryOptions
        {
            Keyword = "AI customer journey",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
            LocalCity = "Delray Beach",
        });

        Assert.DoesNotContain(queries, q => q.Bucket == "local_angle");
    }
}

public sealed class OperatorResearchLocalCityTests
{
    [Fact]
    public void Resolve_prefers_geo_anchor_city_over_generic_search_location()
    {
        var focus = new SiteWritingFocus
        {
            SiteName = "Geek at Your Spot",
            SiteUrl = "https://www.geekatyourspot.com/",
            GeoAnchorNodes = ["Delray Beach, FL, US"],
            ServiceAreaDescription = "Broward County, Palm Beach County, Miami-Dade County",
        };

        var city = OperatorResearchLocalCity.Resolve("United States", focus);

        Assert.Equal("Delray Beach", city);
    }
}

public sealed class OperatorResearchEnricherTests
{
    [Fact]
    public async Task EnrichContextAsync_returns_context_unchanged_when_operator_queries_disabled()
    {
        var enricher = new OperatorResearchEnricher(new FakeOperatorSerpProvider());
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

        var enriched = await enricher.EnrichContextAsync(context);

        Assert.Same(context, enriched);
        Assert.Contains(enriched.CitationCandidates, c => c.Url.Contains("competitor.com", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(enriched.OperatorQueries);
    }

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

            return Task.FromResult(GeekSeo.Application.Results.Result<SerpResult>.Success(new SerpResult
            {
                Keyword = request.Keyword,
                Location = request.Location,
                OrganicResults = [],
                Features = new SerpFeatures(),
                FetchedAt = DateTimeOffset.UtcNow,
            }));
        }
    }
}
