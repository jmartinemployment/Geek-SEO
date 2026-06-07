namespace GeekSeo.Application.Configuration;

/// <summary>
/// Days to serve persisted vendor payloads before re-fetching SerpApi / DataForSEO.
/// </summary>
public static class VendorPersistenceSettings
{
    public const string SerpRetentionDaysEnv = "SEO_VENDOR_SERP_RETENTION_DAYS";
    public const string KeywordRetentionDaysEnv = "SEO_VENDOR_KEYWORD_RETENTION_DAYS";
    public const string SerpCacheDaysEnv = "SEO_VENDOR_SERP_CACHE_DAYS";
    public const string KeywordCacheDaysEnv = "SEO_VENDOR_KEYWORD_CACHE_DAYS";

    public const int DefaultSerpRetentionDays = 30;
    public const int DefaultKeywordRetentionDays = 60;

    public static int SerpRetentionDays =>
        ResolveDays(
            Environment.GetEnvironmentVariable(SerpRetentionDaysEnv),
            Environment.GetEnvironmentVariable(SerpCacheDaysEnv),
            DefaultSerpRetentionDays);

    public static int KeywordRetentionDays =>
        ResolveDays(
            Environment.GetEnvironmentVariable(KeywordRetentionDaysEnv),
            Environment.GetEnvironmentVariable(KeywordCacheDaysEnv),
            DefaultKeywordRetentionDays);

    private static int ResolveDays(string? retentionRaw, string? cacheRaw, int defaultDays) =>
        TryParseDays(retentionRaw) ?? TryParseDays(cacheRaw) ?? defaultDays;

    public static int ParseDays(string? raw, int defaultDays) =>
        TryParseDays(raw) ?? defaultDays;

    private static int? TryParseDays(string? raw) =>
        int.TryParse(raw, out var days) && days > 0 ? days : null;
}
