namespace GeekSeo.Application.Configuration;

public static class VendorPersistenceSettings
{
    public const string SerpCacheDaysEnv = "SEO_VENDOR_SERP_CACHE_DAYS";
    public const string KeywordCacheDaysEnv = "SEO_VENDOR_KEYWORD_CACHE_DAYS";

    public const int DefaultSerpCacheDays = 30;
    public const int DefaultKeywordCacheDays = 60;

    public static int SerpRetentionDays =>
        Parse(Environment.GetEnvironmentVariable(SerpCacheDaysEnv), DefaultSerpCacheDays);

    public static int KeywordRetentionDays =>
        Parse(Environment.GetEnvironmentVariable(KeywordCacheDaysEnv), DefaultKeywordCacheDays);

    private static int Parse(string? raw, int defaultDays) =>
        int.TryParse(raw, out var days) && days > 0 ? days : defaultDays;
}
