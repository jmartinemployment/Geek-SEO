namespace SiteAnalyzer2.Api;

public static class CorsConfiguration
{
    public static string[] ResolveAllowedOrigins()
    {
        var setting = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        if (string.IsNullOrWhiteSpace(setting))
        {
            throw new InvalidOperationException(
                "CORS_ORIGINS is required. Set a comma-separated list of allowed browser origins " +
                "(e.g. https://your-geek-seo-app.example.com,http://localhost:5051). " +
                "Import script uses server-side POST and does not require CORS.");
        }

        var origins = setting
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray();

        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "CORS_ORIGINS is set but no origins were parsed. Provide at least one origin.");
        }

        return origins;
    }
}
