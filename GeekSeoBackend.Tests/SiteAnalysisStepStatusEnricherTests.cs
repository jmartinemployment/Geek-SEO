using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepStatusEnricherTests
{
    [Fact]
    public void MergeStepLog_adds_site_crawl_complete_when_missing_from_json_map()
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schema"] = "complete",
            ["site_structure"] = "pending",
        };

        const string stepLog = """
            [
              {"stepNumber":6,"slug":"site_crawl","title":"Site crawl","status":"complete","summary":"ok","outputs":{}}
            ]
            """;

        SiteAnalyzerStepStatusEnricher.MergeStepLog(merged, stepLog);

        Assert.Equal("complete", merged["site_crawl"]);
        Assert.Equal("pending", merged["site_structure"]);
    }

    [Fact]
    public void ApplyLegacyStructureAliases_maps_monolithic_site_structure_to_split_steps()
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["site_structure"] = "complete",
        };

        SiteAnalyzerStepStatusEnricher.ApplyLegacyStructureAliases(merged);

        Assert.Equal("complete", merged["site_crawl"]);
        Assert.Equal("complete", merged["internal_links"]);
        Assert.Equal("complete", merged["url_patterns"]);
    }
}
