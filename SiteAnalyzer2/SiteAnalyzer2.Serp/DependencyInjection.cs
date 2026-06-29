using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiteAnalyzer2.Serp.Providers;

namespace SiteAnalyzer2.Serp;

public static class DependencyInjection
{
    /// <summary>
    /// Api / local dev: google-scraper + fixture (fixture blocked at resolve time when SERP_EXECUTION=external).
    /// </summary>
    public static IServiceCollection AddSerpProviders(this IServiceCollection services)
    {
        RegisterGoogleScraper(services);
        services.AddSingleton<FixtureSerpProvider>();
        services.AddSingleton<SerpProviderResolver>();
        return services;
    }

    /// <summary>
    /// Mac SERP worker: google-scraper only — fixture is not registered.
    /// </summary>
    public static IServiceCollection AddWorkerSerpProviders(this IServiceCollection services)
    {
        RegisterGoogleScraper(services);
        services.AddSingleton<SerpProviderResolver>();
        return services;
    }

    private static void RegisterGoogleScraper(IServiceCollection services)
    {
        services.AddSingleton<GoogleScrapePacing>();
        services.AddSingleton<GooglePlaywrightFetcher>();
        services.AddHttpClient<GoogleScraperProvider>();
    }
}

public class SerpProviderResolver(
    GoogleScraperProvider googleScraper,
    ILogger<SerpProviderResolver> logger,
    FixtureSerpProvider? fixture = null)
{
    public ISerpProvider Resolve(string providerKey)
    {
        SerpProviderPolicy.EnsureResolvable(providerKey);

        var key = providerKey.ToLowerInvariant();
        var implementation = key switch
        {
            SerpProviderPolicy.FixtureKey => "FixtureSerpProvider",
            SerpProviderPolicy.GoogleScraperKey => "GoogleScraperProvider",
            _ => "unknown"
        };

        logger.LogInformation(
            "SERP provider resolve: key={ProviderKey}, implementation={Implementation}",
            providerKey,
            implementation);

        return key switch
        {
            SerpProviderPolicy.FixtureKey => fixture
                ?? throw new InvalidOperationException(SerpProviderPolicy.RejectionMessage(providerKey)),
            SerpProviderPolicy.GoogleScraperKey => googleScraper,
            _ => throw new InvalidOperationException(
                $"Unknown SERP provider '{providerKey}'. Valid keys: {SerpProviderPolicy.FixtureKey}, {SerpProviderPolicy.GoogleScraperKey}.")
        };
    }
}
