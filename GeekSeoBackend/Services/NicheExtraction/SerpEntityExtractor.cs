using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Derives topic-like entity slugs from SERP organic URLs, related searches, and PAA (Gap 3 proxy).
/// </summary>
internal static class SerpEntityExtractor
{
    internal static IReadOnlyList<string> ExtractTopicSlugs(SerpResult serp)
    {
        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in serp.OrganicResults)
        {
            foreach (var slug in SlugsFromUrl(row.Url))
                slugs.Add(slug);
        }

        foreach (var related in serp.RelatedSearches)
            TryAddPhrase(slugs, related);

        foreach (var paa in serp.PeopleAlsoAsk)
            TryAddPhrase(slugs, paa.Question);

        return slugs
            .Where(s => !NoisePaths.IsNoise(s))
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> SlugsFromUrl(string url)
    {
        string path;
        try
        {
            path = new Uri(url).AbsolutePath.Trim('/');
        }
        catch
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(path))
            yield break;

        foreach (var (_, slug) in UrlPatternExtractor.ExtractTopicSegments(path))
        {
            if (!NoisePaths.IsNoise(slug) && slug.Length >= 3)
                yield return slug;
        }
    }

    private static void TryAddPhrase(ISet<string> slugs, string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return;

        var trimmed = phrase.Trim();
        if (trimmed.Length < 4 || trimmed.Length > 80)
            return;

        var slug = NicheAnalyzerService.NameToSlug(trimmed);
        if (slug.Length >= 3 && !NoisePaths.IsNoise(slug))
            slugs.Add(slug);
    }
}
