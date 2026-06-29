namespace SiteAnalyzer2.Services.Pipeline;

public static class SerpFixtureFileCleanup
{
    public static int CleanUnderContentRoot(string contentRoot)
    {
        var serpDir = Path.Combine(contentRoot, "fixtures", "serp");
        if (!Directory.Exists(serpDir))
            return 0;

        var removed = 0;
        foreach (var dir in Directory.EnumerateDirectories(serpDir))
        {
            var name = Path.GetFileName(dir);
            if (!name.EndsWith("_files", StringComparison.OrdinalIgnoreCase))
                continue;

            Directory.Delete(dir, recursive: true);
            removed++;
        }

        foreach (var file in Directory.EnumerateFiles(serpDir, "*.html", SearchOption.AllDirectories))
        {
            File.Delete(file);
            removed++;
        }

        return removed;
    }
}
