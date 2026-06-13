using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheStepRunners;

public sealed record NicheStepDefinition(
    int StepNumber,
    string Slug,
    string Title,
    string Phase,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> ArtifactReads,
    IReadOnlyList<string> ArtifactWrites,
    bool IsOptional = false,
    bool IsTerminal = false);

public static class NicheStepCatalog
{
    public static readonly IReadOnlyList<NicheStepDefinition> Ordered =
    [
        new(1, "schema", "Schema.org", "discover", [], [], ["step_log:schema"]),
        new(2, "site_urls", "Site URLs", "discover", [], [], ["step_log:site_urls"]),
        new(3, "nav", "Navigation", "discover", [], [], ["step_log:nav"], IsOptional: true),
        new(4, "headings", "Homepage headings", "fetch", [], [], ["step_log:headings"]),
        new(5, "page_content", "Page content", "fetch", [], [], ["step_log:page_content"]),
        new(
            6,
            "site_structure",
            "Site structure",
            "fetch",
            [],
            ["step_log:site_urls"],
            ["step_log:site_structure", "profile:crawled_urls"]),
        new(
            7,
            "merging",
            "Topic selection",
            "understand",
            ["schema", "site_urls", "nav", "headings", "page_content", "site_structure"],
            ["step_log:schema", "step_log:site_urls", "step_log:nav", "step_log:headings", "step_log:page_content", "step_log:site_structure"],
            ["step_log:merging", "profile:fusion_snapshot", "profile:topic_candidates"]),
        new(
            8,
            "keywords",
            "Keyword demand",
            "validate",
            ["merging"],
            ["profile:fusion_snapshot"],
            ["step_log:keywords"],
            IsOptional: true),
        new(
            9,
            "serp_validation",
            "SERP validation",
            "validate",
            ["merging"],
            ["profile:fusion_snapshot"],
            ["step_log:serp_validation"],
            IsOptional: true),
        new(
            10,
            "profile",
            "Niche profile",
            "synthesize",
            ["merging", "serp_validation"],
            ["profile:fusion_snapshot", "step_log:schema", "step_log:serp_validation"],
            ["step_log:profile"]),
        new(
            11,
            "local",
            "Local geography",
            "synthesize",
            ["schema", "site_structure", "merging"],
            ["step_log:schema", "step_log:site_structure", "profile:fusion_snapshot"],
            ["step_log:local", "profile:fusion_snapshot"]),
        new(
            12,
            "coverage",
            "Content coverage",
            "synthesize",
            ["schema", "site_structure", "merging"],
            ["step_log:schema", "step_log:site_structure", "profile:fusion_snapshot"],
            ["step_log:coverage", "profile:pillars", "profile:subtopics"]),
        new(
            13,
            "scoring",
            "Authority score",
            "synthesize",
            ["profile", "local", "coverage"],
            ["step_log:profile", "step_log:local", "step_log:coverage", "profile:fusion_snapshot", "profile:pillars", "profile:subtopics"],
            ["step_log:scoring", "profile:summary", "profile:scores", "profile:fusion_snapshot"]),
        new(
            14,
            "complete",
            "Complete",
            "synthesize",
            ["scoring"],
            ["step_log:scoring"],
            ["step_log:complete", "profile:status"],
            IsTerminal: true),
    ];

    public static readonly IReadOnlyDictionary<string, NicheStepDefinition> BySlug =
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

    public static IReadOnlyList<NicheStepDefinitionDto> ToDtos() =>
        Ordered.Select(step => new NicheStepDefinitionDto(
            step.StepNumber,
            step.Slug,
            step.Title,
            step.Phase,
            step.Dependencies,
            step.IsOptional,
            step.IsTerminal)).ToList();
}
