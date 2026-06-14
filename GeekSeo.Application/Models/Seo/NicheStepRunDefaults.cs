namespace GeekSeo.Application.Models.Seo;

/// <summary>Canonical 14-step slug list shared by GeekRepository and GeekSeoBackend.</summary>
public static class NicheStepRunDefaults
{
    public static readonly IReadOnlyList<(int StepNumber, string StepSlug)> Ordered =
    [
        (1, "schema"),
        (2, "site_urls"),
        (3, "nav"),
        (4, "headings"),
        (5, "page_content"),
        (6, "site_structure"),
        (7, "merging"),
        (8, "keywords"),
        (9, "serp_validation"),
        (10, "profile"),
        (11, "local"),
        (12, "coverage"),
        (13, "scoring"),
        (14, "complete"),
    ];

    public static readonly IReadOnlyDictionary<string, int> StepNumberBySlug =
        Ordered.ToDictionary(x => x.StepSlug, x => x.StepNumber, StringComparer.OrdinalIgnoreCase);
}
