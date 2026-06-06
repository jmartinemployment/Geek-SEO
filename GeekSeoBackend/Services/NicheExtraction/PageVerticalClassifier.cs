using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

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

    internal static bool ShouldTreatAsVertical(int level, string text, bool underSectionParent)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            return false;

        if (IsNoiseForVertical(trimmed))
            return false;

        if (level == 3)
            return true;

        if (level != 2)
            return false;

        if (underSectionParent)
            return true;

        return LooksLikeStandaloneVertical(trimmed);
    }

    internal static bool ResetsSectionContext(int level, string text)
    {
        if (level != 2)
            return false;

        var trimmed = text.Trim();
        if (IsSectionParent(trimmed))
            return false;

        return NoisePaths.H2Noise.Contains(trimmed)
               || IsNoiseForVertical(trimmed)
               || !LooksLikeStandaloneVertical(trimmed);
    }

    private static bool IsNoiseForVertical(string text)
    {
        if (NoisePaths.H2Noise.Contains(text))
            return true;

        return NoisePaths.IsNoise(NicheAnalyzerService.NameToSlug(text));
    }

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
