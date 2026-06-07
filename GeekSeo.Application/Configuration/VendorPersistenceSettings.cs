namespace GeekSeo.Application.Configuration;

/// <summary>
/// Days to serve persisted vendor payloads before re-fetching SerpApi / DataForSEO.
/// </summary>
public static class VendorPersistenceSettings
{
    public const string SerpRetentionDaysEnv = "SEO_VENDOR_SERP_RETENTION_DAYS";
    public const string KeywordRetentionDaysEnv = "SEO_VENDOR_KEYWORD_RETENTION_DAYS";

    public const int DefaultSerpRetentionDays = 30;
    public const int DefaultKeywordRetentionDays = 60;

    public static int SerpRetentionDays =>
        ParseDays(Environment.GetEnvironmentVariable(SerpRetentionDaysEnv), DefaultSerpRetentionDays);

    public static int KeywordRetentionDays =>
        ParseDays(Environment.GetEnvironmentVariable(KeywordRetentionDaysEnv), DefaultKeywordRetentionDays);

    public static int ParseDays(string? raw, int defaultDays)
    {
        if (int.TryParse(raw, out var days) && days > 0)
            return days;
        return defaultDays;
    }
}
