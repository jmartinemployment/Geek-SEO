using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheStepRunners;

/// <summary>
/// Canonical dependency map — backend and frontend must match exactly.
/// </summary>
public static class NicheStepDependencies
{
    public static readonly IReadOnlyDictionary<string, string[]> Map =
        new Dictionary<string, string[]>
        {
            ["schema"]          = [],
            ["site_urls"]       = [],
            ["nav"]             = [],
            ["headings"]        = [],
            ["page_content"]    = [],
            ["site_structure"]  = [],
            ["merging"]         = ["schema","site_urls","nav","headings","page_content","site_structure"],
            ["keywords"]        = ["merging"],
            ["serp_validation"] = ["merging"],
            ["profile"]         = ["merging","serp_validation"],
            ["local"]           = ["schema","site_structure","merging"],
            ["coverage"]        = ["schema","site_structure","merging"],
            ["scoring"]         = ["profile","local","coverage"],
            ["complete"]        = ["scoring"],
        };

    /// <summary>Returns all slugs that directly or transitively depend on <paramref name="slug"/>.</summary>
    public static IReadOnlyList<string> GetDownstream(string slug)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();

        foreach (var (candidate, deps) in Map)
        {
            if (deps.Contains(slug, StringComparer.OrdinalIgnoreCase))
                queue.Enqueue(candidate);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current)) continue;
            foreach (var (candidate, deps) in Map)
            {
                if (deps.Contains(current, StringComparer.OrdinalIgnoreCase))
                    queue.Enqueue(candidate);
            }
        }

        return result.ToList();
    }
}

public interface INicheStepRunner
{
    string Slug { get; }
    Task<NicheAnalysisStepLogEntry> RunAsync(
        Guid profileId,
        Guid userId,
        string domain,
        IBrowser? browser,
        CancellationToken ct);
}
