using GeekSeo.Application.Configuration;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Providers.Seo;
using GeekSeoBackend.Providers.Seo.Metering;
using GeekSeoBackend.Providers.Seo.Persistence;
using GeekSeoBackend.Providers.Seo.SerperDev;
using GeekSeoBackend.Providers.Seo.SerpApi;

namespace GeekSeoBackend.Extensions;

/// <summary>
/// Env-driven registration for SEO data providers (Phase 0 — see plan-documents/SEO-PROVIDER-STRATEGY.md).
/// </summary>
public static class SeoProviderRegistration
{
    public const string SerpProviderEnv = "SERP_PROVIDER";
    public const string SerpProviderFallbackEnv = "SERP_PROVIDER_FALLBACK";
    public const string KeywordProviderEnv = "KEYWORD_PROVIDER";
    public const string RankSnapshotProviderEnv = "RANK_SNAPSHOT_PROVIDER";
    public const string SerpApiKeyEnv = "SERPAPI_API_KEY";
    public const string SerperDevApiKeyEnv = "SERPER_DEV_API_KEY";

    public static IServiceCollection AddSeoDataProviders(this IServiceCollection services)
    {
        services.AddHttpClient("DataForSEO", client =>
        {
            client.BaseAddress = new Uri("https://api.dataforseo.com");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("SerpApi", client =>
        {
            client.BaseAddress = new Uri("https://serpapi.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("SerperDev", client =>
        {
            client.BaseAddress = new Uri("https://google.serper.dev/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var config = SeoProviderConfiguration.FromEnvironment();
        services.AddSingleton(config);
        EnsureSerpApiKeyWhenRequired(config);

        RegisterSerpProviderImplementations(services, config);
        RegisterKeywordProvider(services, config);
        services.AddScoped<IKeywordDiscoveryProvider, InternalKeywordDiscoveryProvider>();
        RegisterRankSnapshotImplementations(services, config);

        services.AddScoped<IRankSnapshotProvider>(sp => new MeteredRankSnapshotProvider(
            ResolveRankSnapshotProvider(sp, config),
            sp.GetRequiredService<IUsageMeteringService>(),
            sp.GetRequiredService<ICurrentUserContext>(),
            sp.GetRequiredService<ILogger<MeteredRankSnapshotProvider>>()));

        return services;
    }

    private static void RegisterSerpProviderImplementations(IServiceCollection services, SeoProviderConfiguration config)
    {
        switch (config.SerpProvider)
        {
            case "dataforseo":
                if (!string.IsNullOrEmpty(config.SerpProviderFallback))
                {
                    throw new InvalidOperationException(
                        $"{SerpProviderFallbackEnv} is only supported when {SerpProviderEnv}=serpapi.");
                }

                services.AddScoped<DataForSEOSerpProvider>();
                break;
            case "serpapi":
                services.AddScoped<SerpApiSerpProvider>();
                if (config.SerpProviderFallback == "dataforseo")
                {
                    services.AddScoped<DataForSEOSerpProvider>();
                    services.AddScoped<FallbackSerpProvider>();
                }
                else if (!string.IsNullOrEmpty(config.SerpProviderFallback))
                {
                    throw new InvalidOperationException(
                        $"Invalid {SerpProviderFallbackEnv}={config.SerpProviderFallback}. Allowed with serpapi: dataforseo.");
                }

                break;
            case "serpdev":
                services.AddScoped<SerperDevSerpProvider>();
                break;
            case "geek":
                throw new InvalidOperationException(
                    "SERP_PROVIDER=geek is not implemented yet. Add GeekSerpProvider (Phase C) or use SERP_PROVIDER=serpdev.");
            default:
                throw new InvalidOperationException(
                    $"Invalid {SerpProviderEnv}={config.SerpProvider}. Allowed: dataforseo, serpapi, serpdev, geek.");
        }

        services.AddScoped<ISerpProvider>(sp => new DatabaseBackedSerpProvider(
            ResolveInnerSerpProvider(sp, config),
            sp.GetRequiredService<ISerpCacheRepository>()));
    }

    private static ISerpProvider ResolveInnerSerpProvider(IServiceProvider sp, SeoProviderConfiguration config) =>
        config.SerpProvider switch
        {
            "dataforseo" => sp.GetRequiredService<DataForSEOSerpProvider>(),
            "serpapi" when config.SerpProviderFallback == "dataforseo" => sp.GetRequiredService<FallbackSerpProvider>(),
            "serpapi" => sp.GetRequiredService<SerpApiSerpProvider>(),
            "serpdev" => sp.GetRequiredService<SerperDevSerpProvider>(),
            _ => throw new InvalidOperationException($"Unhandled {SerpProviderEnv}={config.SerpProvider}"),
        };

    private static void EnsureSerpApiKeyWhenRequired(SeoProviderConfiguration config)
    {
        if (config.SerpProvider == "serpapi" || config.RankSnapshotProvider == "serpapi")
        {
            if (!config.SerpApiKeyConfigured)
                throw new InvalidOperationException(
                    $"{SerpApiKeyEnv} is required when SERP_PROVIDER or RANK_SNAPSHOT_PROVIDER is serpapi.");
        }

        if (config.SerpProvider == "serpdev")
        {
            if (!config.SerperDevApiKeyConfigured)
                throw new InvalidOperationException(
                    $"{SerperDevApiKeyEnv} is required when SERP_PROVIDER=serpdev.");
        }
    }

    private static void RegisterKeywordProvider(IServiceCollection services, SeoProviderConfiguration config)
    {
        switch (config.KeywordProvider)
        {
            case "none":
                // Keyword enrichment disabled — niche analyzer uses SERP (PAA + related searches) instead.
                services.AddScoped<IKeywordProvider, NoOpKeywordProvider>();
                break;
            case "dataforseo":
                services.AddScoped<DataForSEOKeywordProvider>();
                services.AddScoped<IKeywordProvider>(sp => new DatabaseBackedKeywordProvider(
                    sp.GetRequiredService<DataForSEOKeywordProvider>(),
                    sp.GetRequiredService<IKeywordVendorSnapshotRepository>()));
                break;
            case "gsc_ads":
            case "geek":
                throw new InvalidOperationException(
                    $"KEYWORD_PROVIDER={config.KeywordProvider} is not implemented yet. Use KEYWORD_PROVIDER=none or dataforseo.");
            default:
                throw new InvalidOperationException(
                    $"Invalid {KeywordProviderEnv}={config.KeywordProvider}. Allowed: none, dataforseo.");
        }
    }

    private static void RegisterRankSnapshotImplementations(IServiceCollection services, SeoProviderConfiguration config)
    {
        switch (config.RankSnapshotProvider)
        {
            case "dataforseo":
                services.AddScoped<DataForSeoRankSnapshotProvider>();
                break;
            case "serpapi":
                services.AddScoped<SerpApiRankSnapshotProvider>();
                break;
            case "geek":
                throw new InvalidOperationException(
                    "RANK_SNAPSHOT_PROVIDER=geek is not implemented yet. Use dataforseo until Phase C.");
            default:
                throw new InvalidOperationException(
                    $"Invalid {RankSnapshotProviderEnv}={config.RankSnapshotProvider}. Allowed: dataforseo, serpapi, geek.");
        }
    }

    private static IRankSnapshotProvider ResolveRankSnapshotProvider(
        IServiceProvider sp,
        SeoProviderConfiguration config) =>
        config.RankSnapshotProvider switch
        {
            "dataforseo" => sp.GetRequiredService<DataForSeoRankSnapshotProvider>(),
            "serpapi" => sp.GetRequiredService<SerpApiRankSnapshotProvider>(),
            _ => throw new InvalidOperationException($"Unhandled {RankSnapshotProviderEnv}={config.RankSnapshotProvider}"),
        };
}

/// <summary>Resolved provider env (no secrets).</summary>
public sealed class SeoProviderConfiguration
{
    public required string SerpProvider { get; init; }
    public string? SerpProviderFallback { get; init; }
    public required string KeywordProvider { get; init; }
    public required string RankSnapshotProvider { get; init; }
    public bool DataForSeoCredentialsConfigured { get; init; }
    public bool SerpApiKeyConfigured { get; init; }
    public bool SerperDevApiKeyConfigured { get; init; }
    public int SerpRetentionDays { get; init; }
    public int KeywordRetentionDays { get; init; }

    public static SeoProviderConfiguration FromEnvironment()
    {
        var serpFallback = Environment.GetEnvironmentVariable(SeoProviderRegistration.SerpProviderFallbackEnv);
        return new SeoProviderConfiguration
        {
            SerpProvider = Normalize(
                Environment.GetEnvironmentVariable(SeoProviderRegistration.SerpProviderEnv),
                "dataforseo"),
            SerpProviderFallback = string.IsNullOrWhiteSpace(serpFallback) ? null : serpFallback.Trim().ToLowerInvariant(),
            KeywordProvider = Normalize(
                Environment.GetEnvironmentVariable(SeoProviderRegistration.KeywordProviderEnv),
                "dataforseo"),
            RankSnapshotProvider = Normalize(
                Environment.GetEnvironmentVariable(SeoProviderRegistration.RankSnapshotProviderEnv),
                "dataforseo"),
            DataForSeoCredentialsConfigured = DataForSeoClient.TryGetCredentials(out _, out _),
            SerpApiKeyConfigured = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(SeoProviderRegistration.SerpApiKeyEnv)),
            SerperDevApiKeyConfigured = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(SeoProviderRegistration.SerperDevApiKeyEnv)),
            SerpRetentionDays = VendorPersistenceSettings.SerpRetentionDays,
            KeywordRetentionDays = VendorPersistenceSettings.KeywordRetentionDays,
        };
    }

    private static string Normalize(string? raw, string defaultValue) =>
        string.IsNullOrWhiteSpace(raw) ? defaultValue : raw.Trim().ToLowerInvariant();
}
