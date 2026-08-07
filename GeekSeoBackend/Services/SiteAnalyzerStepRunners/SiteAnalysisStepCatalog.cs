using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

public sealed record SiteAnalysisStepDefinition(
    int StepNumber,
    string Slug,
    string Title,
    string Phase,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> ArtifactReads,
    IReadOnlyList<string> ArtifactWrites,
    bool IsOptional = false,
    bool IsTerminal = false);

public static class SiteAnalysisStepCatalog
{
    public static readonly IReadOnlyList<SiteAnalysisStepDefinition> Ordered =
    [
        new(1, "schema", "Schema.org", "discover", [], [], ["step_log:schema"]),
        new(2, "site_urls", "Site URLs", "discover", [], [], ["step_log:site_urls"]),
        new(3, "nav", "Navigation", "discover", [], [], ["step_log:nav"], IsOptional: true),
        new(4, "headings", "Homepage headings", "fetch", [], [], ["step_log:headings"]),
        new(5, "page_content", "Page content", "fetch", [], [], ["step_log:page_content"]),
        new(
            6,
            "site_crawl",
            "Site crawl",
            "fetch",
            ["site_urls"],
            ["step_log:site_urls"],
            ["step_log:site_crawl", "profile:crawled_urls", "profile:site_structure_pages"]),
        new(
            7,
            "internal_links",
            "Internal links",
            "fetch",
            ["site_crawl"],
            ["step_log:site_crawl", "profile:site_structure_pages"],
            ["step_log:internal_links", "profile:site_structure_links"]),
        new(
            8,
            "url_patterns",
            "URL patterns",
            "fetch",
            ["site_crawl", "site_urls"],
            ["step_log:site_crawl", "step_log:site_urls", "profile:site_structure_pages", "profile:site_structure_links"],
            ["step_log:url_patterns", "profile:site_structure_patterns"]),
        new(
            9,
            "complete",
            "Complete",
            "synthesize",
            ["schema", "site_urls", "nav", "headings", "page_content", "site_crawl", "internal_links", "url_patterns"],
            [
                "step_log:schema", "step_log:site_urls", "step_log:nav", "step_log:headings", "step_log:page_content",
                "step_log:site_crawl", "step_log:internal_links", "step_log:url_patterns",
            ],
            ["step_log:complete", "profile:status"],
            IsTerminal: true),
    ];

    public static readonly IReadOnlyDictionary<string, SiteAnalysisStepDefinition> BySlug =
        Ordered.ToDictionary(step => step.Slug, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<string, string[]> DependencyMap =
        Ordered.ToDictionary(
            step => step.Slug,
            step => step.Dependencies.ToArray(),
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GetDownstream(string slug)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        foreach (var step in Ordered)
        {
            if (step.Dependencies.Contains(slug, StringComparer.OrdinalIgnoreCase))
                queue.Enqueue(step.Slug);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
                continue;

            foreach (var step in Ordered)
            {
                if (step.Dependencies.Contains(current, StringComparer.OrdinalIgnoreCase))
                    queue.Enqueue(step.Slug);
            }
        }

        return result.OrderBy(s => BySlug[s].StepNumber).ToList();
    }

    public static IReadOnlyList<SiteAnalysisStepDefinitionDto> ToDtos() =>
        Ordered.Select(step => new SiteAnalysisStepDefinitionDto(
            step.StepNumber,
            step.Slug,
            step.Title,
            step.Phase,
            step.Dependencies,
            step.IsOptional,
            step.IsTerminal)).ToList();
}
