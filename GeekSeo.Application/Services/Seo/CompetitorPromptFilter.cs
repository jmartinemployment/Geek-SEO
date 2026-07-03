using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Converts competitor/SERP titles into structural subtopic patterns — never verbatim titles or brand names.
/// </summary>
public static partial class CompetitorPromptFilter
{
    public static IReadOnlyList<string> BuildStructuralPatterns(WritingResearchContext research)
    {
        var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var brandStems = CollectBrandStems(research);

        foreach (var competitor in research.Competitors)
        {
            if (!string.IsNullOrWhiteSpace(competitor.H1))
                TryAddPattern(patterns, competitor.H1, brandStems);

            foreach (var heading in competitor.Headings.Where(h => h.Level <= 3))
            {
                if (!string.IsNullOrWhiteSpace(heading.Text))
                    TryAddPattern(patterns, heading.Text, brandStems);
            }
        }

        foreach (var organic in research.Organic.Take(8))
        {
            if (!string.IsNullOrWhiteSpace(organic.Title))
                TryAddPattern(patterns, organic.Title, brandStems);
        }

        return patterns.Take(10).ToList();
    }

    private static void TryAddPattern(HashSet<string> patterns, string title, HashSet<string> brandStems)
    {
        var normalized = NormalizeTitle(title);
        if (normalized.Length < 8)
            return;

        if (brandStems.Any(stem => normalized.Contains(stem, StringComparison.OrdinalIgnoreCase)))
            return;

        patterns.Add(ClassifyStructure(normalized));
    }

    private static string ClassifyStructure(string title)
    {
        var lower = title.ToLowerInvariant();

        if (VsPattern().IsMatch(lower))
            return "comparison-style subtopic (contrast two approaches without naming vendors)";

        if (lower.StartsWith("how ", StringComparison.Ordinal)
            || lower.StartsWith("what ", StringComparison.Ordinal)
            || lower.StartsWith("why ", StringComparison.Ordinal)
            || lower.StartsWith("when ", StringComparison.Ordinal))
        {
            return "how-to procedural subtopic (steps a buyer can act on)";
        }

        if (lower.Contains("cost", StringComparison.Ordinal)
            || lower.Contains("price", StringComparison.Ordinal)
            || lower.Contains("pricing", StringComparison.Ordinal))
        {
            return "cost or ROI question subtopic";
        }

        if (lower.Contains("checklist", StringComparison.Ordinal)
            || lower.Contains("steps", StringComparison.Ordinal)
            || lower.Contains("guide", StringComparison.Ordinal))
        {
            return "implementation checklist subtopic";
        }

        return "buyer decision subtopic (original wording — do not mirror source titles)";
    }

    private static HashSet<string> CollectBrandStems(WritingResearchContext research)
    {
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var competitor in research.Competitors)
            AddHostStem(stems, competitor.Url);

        foreach (var organic in research.Organic)
            AddHostStem(stems, organic.Url);

        return stems.Where(s => s.Length >= 4).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void AddHostStem(HashSet<string> stems, string? url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
            return;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        var label = host.Split('.')[0];
        if (label.Length >= 4)
            stems.Add(label);
    }

    private static string NormalizeTitle(string title) =>
        WhitespaceRegex().Replace(title.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\bvs\.?\b|\bversus\b", RegexOptions.IgnoreCase)]
    private static partial Regex VsPattern();
}
