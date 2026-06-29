using SiteAnalyzer2.Serp;

namespace SiteAnalyzer2.Services.Pipeline;

public static class SerpFixtureLocator
{
    public static string ResolveDefaultHtmlPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "fixtures", "serp", SerpCanonicalFixture.HtmlFileName),
            FindRepoFixture(SerpCanonicalFixture.HtmlFileName)
        };

        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;
        }

        throw new FileNotFoundException(
            $"SERP canonical fixture not found. Save Google results HTML as SiteAnalyzer2/fixtures/serp/{SerpCanonicalFixture.HtmlFileName}.");
    }

    public static string? FindRepoFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var siteAnalyzerFixture = Path.Combine(dir.FullName, "SiteAnalyzer2", "fixtures", "serp", fileName);
            if (File.Exists(siteAnalyzerFixture))
                return siteAnalyzerFixture;

            var candidate = Path.Combine(dir.FullName, "tests", "fixtures", "serp", fileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
