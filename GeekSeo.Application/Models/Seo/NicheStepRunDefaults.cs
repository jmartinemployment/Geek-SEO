namespace GeekSeo.Application.Models.Seo;

/// <summary>Canonical step slug list shared by GeekRepository and GeekSeoBackend.</summary>
public static class NicheStepRunDefaults
{
    public static readonly IReadOnlyList<(int StepNumber, string StepSlug)> Ordered =
    [
        (1, "schema"),
        (2, "site_urls"),
        (3, "nav"),
        (4, "headings"),
        (5, "page_content"),
        (6, "site_crawl"),
        (7, "internal_links"),
        (8, "url_patterns"),
        (9, "merging"),
        (10, "keywords"),
        (11, "serp_validation"),
        (12, "profile"),
        (13, "local"),
        (14, "coverage"),
        (15, "scoring"),
        (16, "complete"),
    ];

    public static readonly IReadOnlyDictionary<string, int> StepNumberBySlug =
        Ordered.ToDictionary(x => x.StepSlug, x => x.StepNumber, StringComparer.OrdinalIgnoreCase);
}
