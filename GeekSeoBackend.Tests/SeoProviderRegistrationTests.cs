using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Providers.Seo;
using GeekSeoBackend.Providers.Seo.Metering;
using GeekSeoBackend.Providers.Seo.Persistence;
using GeekSeoBackend.Providers.Seo.SerpApi;
using GeekSeo.Persistence.Entities;
using SubscriptionTier = GeekSeo.Application.Constants.Seo.SubscriptionTier;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeekSeoBackend.Tests;

public sealed class SeoProviderRegistrationTests
{
    [Fact]
    public void AddSeoDataProviders_with_defaults_registers_database_wrapped_providers()
    {
        using var env = EnvScope.For(new Dictionary<string, string?>
        {
            [SeoProviderRegistration.SerpProviderEnv] = null,
            [SeoProviderRegistration.KeywordProviderEnv] = null,
            [SeoProviderRegistration.RankSnapshotProviderEnv] = null,
            [SeoProviderRegistration.SerpProviderFallbackEnv] = null,
        });

        var services = CreateServiceCollection();
        services.AddSeoDataProviders();
        using var sp = services.BuildServiceProvider();

        Assert.IsType<DatabaseBackedSerpProvider>(sp.GetRequiredService<ISerpProvider>());
        Assert.IsType<DatabaseBackedKeywordProvider>(sp.GetRequiredService<IKeywordProvider>());
        Assert.IsType<MeteredRankSnapshotProvider>(sp.GetRequiredService<IRankSnapshotProvider>());
        Assert.IsType<DataForSeoRankSnapshotProvider>(sp.GetRequiredService<DataForSeoRankSnapshotProvider>());

        var config = sp.GetRequiredService<SeoProviderConfiguration>();
        Assert.Equal("dataforseo", config.SerpProvider);
        Assert.Equal("dataforseo", config.KeywordProvider);
        Assert.Equal("dataforseo", config.RankSnapshotProvider);
        Assert.Equal(30, config.SerpRetentionDays);
        Assert.Equal(60, config.KeywordRetentionDays);
    }

    [Fact]
    public void AddSeoDataProviders_serpapi_without_api_key_fails_at_startup()
    {
        using var env = EnvScope.For(new Dictionary<string, string?>
        {
            [SeoProviderRegistration.SerpProviderEnv] = "serpapi",
            [SeoProviderRegistration.SerpApiKeyEnv] = null,
        });

        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSeoDataProviders());
        Assert.Contains(SeoProviderRegistration.SerpApiKeyEnv, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSeoDataProviders_serpapi_with_key_registers_serpapi_implementations()
    {
        using var env = EnvScope.For(new Dictionary<string, string?>
        {
            [SeoProviderRegistration.SerpProviderEnv] = "serpapi",
            [SeoProviderRegistration.RankSnapshotProviderEnv] = "serpapi",
            [SeoProviderRegistration.SerpApiKeyEnv] = "test-key",
            [SeoProviderRegistration.SerpProviderFallbackEnv] = null,
        });

        var services = CreateServiceCollection();
        services.AddSeoDataProviders();
        using var sp = services.BuildServiceProvider();

        Assert.IsType<DatabaseBackedSerpProvider>(sp.GetRequiredService<ISerpProvider>());
        Assert.IsType<MeteredRankSnapshotProvider>(sp.GetRequiredService<IRankSnapshotProvider>());
        Assert.IsType<SerpApiRankSnapshotProvider>(sp.GetRequiredService<SerpApiRankSnapshotProvider>());
    }

    [Fact]
    public void AddSeoDataProviders_serpapi_with_fallback_registers_fallback_wrapper()
    {
        using var env = EnvScope.For(new Dictionary<string, string?>
        {
            [SeoProviderRegistration.SerpProviderEnv] = "serpapi",
            [SeoProviderRegistration.SerpApiKeyEnv] = "test-key",
            [SeoProviderRegistration.SerpProviderFallbackEnv] = "dataforseo",
            ["DATAFORSEO_LOGIN"] = "user",
            ["DATAFORSEO_PASSWORD"] = "pass",
        });

        var services = CreateServiceCollection();
        services.AddSeoDataProviders();
        using var sp = services.BuildServiceProvider();

        var serp = sp.GetRequiredService<ISerpProvider>();
        Assert.IsType<DatabaseBackedSerpProvider>(serp);
    }

    [Fact]
    public void SeoProviderConfiguration_FromEnvironment_reflects_credential_and_cache_flags()
    {
        using var env = EnvScope.For(new Dictionary<string, string?>
        {
            ["DATAFORSEO_LOGIN"] = "user",
            ["DATAFORSEO_PASSWORD"] = "pass",
            [SeoProviderRegistration.SerpApiKeyEnv] = "key",
            ["SEO_VENDOR_SERP_RETENTION_DAYS"] = "45",
            ["SEO_VENDOR_KEYWORD_RETENTION_DAYS"] = "90",
        });

        var config = SeoProviderConfiguration.FromEnvironment();
        Assert.True(config.DataForSeoCredentialsConfigured);
        Assert.True(config.SerpApiKeyConfigured);
        Assert.Equal(45, config.SerpRetentionDays);
        Assert.Equal(90, config.KeywordRetentionDays);
    }

    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<ICurrentUserContext, TestUserContext>();
        services.AddSingleton<IUsageMeteringService, TestUsageMetering>();
        services.AddSingleton<ISerpCacheRepository, TestSerpCacheRepository>();
        services.AddSingleton<IKeywordVendorSnapshotRepository, TestKeywordVendorSnapshotRepository>();
        return services;
    }

    private sealed class TestSerpCacheRepository : ISerpCacheRepository
    {
        public Task<Result<SeoSerpResult?>> GetAsync(
            string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult?>.Success(null));

        public Task<Result<SeoSerpResult>> UpsertAsync(
            string keyword, string location, string languageCode,
            SerpResult serp, SerpBenchmarksPayload benchmarks,
            CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSerpResult>.Failure("test stub"));

        public Task<Result> DeleteAsync(
            string keyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class TestKeywordVendorSnapshotRepository : IKeywordVendorSnapshotRepository
    {
        public Task<Result<SeoKeywordVendorSnapshot?>> GetAsync(
            string seedKeyword, string location, string languageCode, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoKeywordVendorSnapshot?>.Success(null));

        public Task<Result<SeoKeywordVendorSnapshot>> UpsertAsync(
            SeoKeywordVendorSnapshot entry, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoKeywordVendorSnapshot>.Success(entry));
    }

    private sealed class TestUserContext : ICurrentUserContext
    {
        public Guid UserId => Guid.Empty;
        public string? Email => null;
        public bool IsAuthenticated => false;
    }

    private sealed class TestUsageMetering : IUsageMeteringService
    {
        public Task<Result<int>> GetUsageAsync(Guid userId, string feature, CancellationToken ct = default) =>
            Task.FromResult(Result<int>.Success(0));

        public Task<Result<int>> GetLimitAsync(SubscriptionTier tier, string feature, CancellationToken ct = default) =>
            Task.FromResult(Result<int>.Success(int.MaxValue));

        public Task<Result> IncrementAsync(Guid userId, string feature, int amount = 1, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());

        public Task<Result> EnsureWithinLimitAsync(
            Guid userId, SubscriptionTier tier, string feature, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _prior = [];

        public static EnvScope For(IReadOnlyDictionary<string, string?> values)
        {
            var scope = new EnvScope();
            foreach (var (key, value) in values)
            {
                scope._prior[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }

            return scope;
        }

        public void Dispose()
        {
            foreach (var (key, value) in _prior)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
