using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Decides whether a homepage heading is a service vertical (page_vertical) vs a generic phrase.
/// H3 sections are vertical by default; H2 requires section context or a vertical-like label.
/// </summary>
internal static class PageVerticalClassifier
{
    private const int MinLength = 4;
    private const int MaxLength = 80;
    private const int MaxStandaloneWords = 5;

    private static readonly HashSet<string> SectionParentPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "industries",
        "industries we serve",
        "who we serve",
        "sectors",
        "our services",
        "services",
        "solutions",
        "use cases",
        "verticals",
        "markets",
        "clients we serve",
        "industry expertise",
        "specialties",
        "specializations",
    };

    internal static bool IsSectionParent(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (SectionParentPhrases.Contains(normalized))
            return true;

        return normalized.EndsWith(" we serve", StringComparison.OrdinalIgnoreCase)
               || normalized.EndsWith(" industries", StringComparison.OrdinalIgnoreCase);
    }

    // Vertical/section filters removed: if heading its valid, treat all h2/h3 as potential verticals.
    internal static bool ShouldTreatAsVertical(int level, string text, bool underSectionParent) => level is 2 or 3 && !string.IsNullOrWhiteSpace(text.Trim());

    internal static bool ResetsSectionContext(int level, string text) => false;

    private static bool LooksLikeStandaloneVertical(string text)
    {
        if (text.Contains('?', StringComparison.Ordinal))
            return false;

        var lower = text.ToLowerInvariant();
        if (lower.StartsWith("how ", StringComparison.Ordinal)
            || lower.StartsWith("why ", StringComparison.Ordinal)
            || lower.StartsWith("what ", StringComparison.Ordinal)
            || lower.StartsWith("get ", StringComparison.Ordinal)
            || lower.StartsWith("learn ", StringComparison.Ordinal)
            || lower.StartsWith("contact", StringComparison.Ordinal)
            || lower.StartsWith("about ", StringComparison.Ordinal)
            || lower.StartsWith("meet ", StringComparison.Ordinal)
            || lower.StartsWith("welcome", StringComparison.Ordinal))
            return false;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0 || words.Length > MaxStandaloneWords)
            return false;

        if (words.Length == 1)
            return char.IsLetter(words[0][0]);

        return !IsSectionParent(text);
    }

    private static string Normalize(string text) =>
        text.Trim().TrimEnd(':').Trim();
}
