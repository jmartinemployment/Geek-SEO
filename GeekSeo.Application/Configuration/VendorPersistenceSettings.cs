namespace GeekSeo.Application.Configuration;

/// <summary>
/// Days to serve persisted vendor payloads before re-fetching SerpApi / DataForSEO.
/// Set <see cref="RetentionDaysEnv"/> on Railway (one value for everything).
/// </summary>
public static class VendorPersistenceSettings
{
    /// <summary>Primary ops knob — one number for SERP + keyword persistence.</summary>
    public const string RetentionDaysEnv = "SEO_VENDOR_RETENTION_DAYS";

    public const string SerpRetentionDaysEnv = "SEO_VENDOR_SERP_RETENTION_DAYS";
    public const string KeywordRetentionDaysEnv = "SEO_VENDOR_KEYWORD_RETENTION_DAYS";

    /// <summary>Legacy names (still read if present on Railway).</summary>
    public const string SerpCacheDaysEnv = "SEO_VENDOR_SERP_CACHE_DAYS";
    public const string KeywordCacheDaysEnv = "SEO_VENDOR_KEYWORD_CACHE_DAYS";

    public const int DefaultSerpRetentionDays = 30;
    public const int DefaultKeywordRetentionDays = 60;

    /// <summary>Effective unified retention when only one knob is set (health / docs).</summary>
    public static int RetentionDays
    {
        get
        {
            var unified = TryParseDays(Environment.GetEnvironmentVariable(RetentionDaysEnv));
            if (unified.HasValue)
                return unified.Value;

            var serp = SerpRetentionDays;
            var keywords = KeywordRetentionDays;
            return serp == keywords ? serp : serp;
        }
    }

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

    private static int ResolveDays(string? specificRaw, string? legacyRaw, int defaultDays)
    {
        var unified = TryParseDays(Environment.GetEnvironmentVariable(RetentionDaysEnv));
        var specific = TryParseDays(specificRaw);
        var legacy = TryParseDays(legacyRaw);
        return specific ?? legacy ?? unified ?? defaultDays;
    }

    public static int ParseDays(string? raw, int defaultDays) =>
        TryParseDays(raw) ?? defaultDays;

    private static int? TryParseDays(string? raw) =>
        int.TryParse(raw, out var days) && days > 0 ? days : null;
}
