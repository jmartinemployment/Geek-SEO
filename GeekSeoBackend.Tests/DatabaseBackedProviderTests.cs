using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Providers.Seo.Persistence;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class DatabaseBackedProviderTests
{
    [Fact]
    public async Task DatabaseBackedSerpProvider_returns_cached_row_without_calling_vendor()
    {
        var inner = new CountingSerpProvider();
        var cache = new InMemorySerpCache();
        var row = new SeoSerpResult
        {
            Id = Guid.NewGuid(),
            Keyword = "plumber",
            Location = "Miami, FL",
            LanguageCode = "en",
            ResultsJson = """{"organicResults":[{"position":1,"url":"https://a.com","title":"A","snippet":"d"}]}""",
            SerpFeaturesJson = "{}",
            PeopleAlsoAskJson = "[]",
            RelatedSearchesJson = "[]",
            FetchedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(29),
        };
        cache.Row = row;

        var provider = new DatabaseBackedSerpProvider(inner, cache);
        var result = await provider.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = "plumber",
            Location = "Miami, FL",
            LanguageCode = "en",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, inner.CallCount);
        Assert.Single(result.Value!.OrganicResults);
    }

    [Fact]
    public async Task DatabaseBackedSerpProvider_calls_vendor_when_cache_expired()
    {
        var inner = new CountingSerpProvider();
        var cache = new InMemorySerpCache
        {
            Row = new SeoSerpResult
            {
                Id = Guid.NewGuid(),
                Keyword = "plumber",
                Location = "Miami, FL",
                LanguageCode = "en",
                ResultsJson = """{"organicResults":[{"position":1,"url":"https://a.com","title":"A","snippet":"d"}]}""",
                SerpFeaturesJson = "{}",
                PeopleAlsoAskJson = "[]",
                RelatedSearchesJson = "[]",
                FetchedAt = DateTimeOffset.UtcNow.AddDays(-31),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            },
        };

        var provider = new DatabaseBackedSerpProvider(inner, cache);
        _ = await provider.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = "plumber",
            Location = "Miami, FL",
            LanguageCode = "en",
        });

        Assert.Equal(1, inner.CallCount);
        Assert.Equal(1, cache.UpsertCount);
    }

    [Fact]
    public async Task DatabaseBackedKeywordProvider_returns_snapshot_without_calling_vendor()
    {
        var inner = new CountingKeywordProvider();
        var snapshots = new InMemoryKeywordSnapshots
        {
            Row = new SeoKeywordVendorSnapshot
            {
                Id = Guid.NewGuid(),
                SeedKeyword = "plumber",
                Location = "Miami, FL",
                LanguageCode = "en",
                Provider = "dataforseo",
                ResultsJson = """[{"keyword":"plumber near me","searchVolume":100,"keywordDifficulty":20,"cpcUsd":1.5,"competition":"LOW"}]""",
                FetchedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(59),
            },
        };

        var provider = new DatabaseBackedKeywordProvider(inner, snapshots);
        var result = await provider.GetKeywordSuggestionsAsync("plumber", "Miami, FL", 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, inner.CallCount);
        Assert.Single(result.Value!);
    }

    private sealed class CountingSerpProvider : ISerpProvider
    {
        public int CallCount { get; private set; }
        public string ProviderName => "test";

        public Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(Result<SerpResult>.Success(new SerpResult
            {
                Keyword = request.Keyword,
                Location = request.Location,
                OrganicResults =
                [
                    new SerpOrganicResult
                    {
                        Position = 1,
                        Url = "https://live.com",
                        Title = "Live",
                        Snippet = "live",
                    },
                ],
                Features = new SerpFeatures(),
                FetchedAt = DateTimeOffset.UtcNow,
            }));
        }
    }

    private sealed class CountingKeywordProvider : IKeywordProvider
    {
        public int CallCount { get; private set; }
        public string ProviderName => "dataforseo";

        public Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(
            string seedKeyword, string location, int count, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(Result<IReadOnlyList<KeywordResult>>.Success(
                new List<KeywordResult>
                {
                    new()
                    {
                        Keyword = "live",
                        SearchVolume = 1,
                        KeywordDifficulty = 1,
                        CpcUsd = 1,
                        Competition = "LOW",
                    },
                }));
        }
    }

    private sealed class InMemorySerpCache : ISerpCacheRepository
    {
        public SeoSerpResult? Row { get; set; }
        public int UpsertCount { get; private set; }

        public Task<Result<SeoSerpResult?>> GetAsync(
            string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult?>.Success(Row));

        public Task<Result<SeoSerpResult>> UpsertAsync(
            string keyword, string location, string languageCode,
            SerpResult serp, SerpBenchmarksPayload benchmarks,
            CancellationToken ct = default)
        {
            UpsertCount++;
            return Task.FromResult(Result<SeoSerpResult>.Success(Row ?? new SeoSerpResult
            {
                Id = Guid.NewGuid(),
                Keyword = keyword,
                Location = location,
                LanguageCode = languageCode,
            }));
        }

        public Task<Result> DeleteAsync(
            string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class InMemoryKeywordSnapshots : IKeywordVendorSnapshotRepository
    {
        public SeoKeywordVendorSnapshot? Row { get; set; }

        public Task<Result<SeoKeywordVendorSnapshot?>> GetAsync(
            string seedKeyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoKeywordVendorSnapshot?>.Success(Row));

        public Task<Result<SeoKeywordVendorSnapshot>> UpsertAsync(
            SeoKeywordVendorSnapshot entry, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoKeywordVendorSnapshot>.Success(entry));
    }
}
