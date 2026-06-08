namespace GeekSeo.Application.Configuration;

public static class VendorPersistenceSettings
{
    public const string SerpCacheDaysEnv = "SEO_VENDOR_SERP_CACHE_DAYS";
    public const string KeywordCacheDaysEnv = "SEO_VENDOR_KEYWORD_CACHE_DAYS";

    public static int SerpRetentionDays => Parse(SerpCacheDaysEnv);
    public static int KeywordRetentionDays => Parse(KeywordCacheDaysEnv);

    private static int Parse(string envKey)
    {
        var raw = Environment.GetEnvironmentVariable(envKey);
        if (int.TryParse(raw, out var days) && days > 0)
            return days;
        throw new InvalidOperationException($"{envKey} is required and must be a positive integer.");
    }
}
