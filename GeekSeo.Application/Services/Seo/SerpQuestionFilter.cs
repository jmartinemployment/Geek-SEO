using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Filters resource-seeker and off-intent SERP questions (PDF, template, generator, etc.)
/// from FAQs, cluster spokes, and prompts.
/// </summary>
public static partial class SerpQuestionFilter
{
    private static readonly HashSet<string> KeywordStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "for", "to", "of", "in", "on", "how", "what", "is", "are",
        "your", "you", "with", "at", "by", "from", "ai", "implement", "implementation",
    };
    public static readonly string[] DefaultBlocklist =
    [
        "pdf",
        "template",
        "generator",
        "worksheet",
        "cheat sheet",
        "cheat-sheet",
        "download",
        "reddit",
        "course",
        "jobs",
        "salary",
        "ppt",
        "powerpoint",
        "where can i find",
        "where to find",
        "free download",
    ];

    public static bool IsBlocked(string phrase, IEnumerable<string>? extraBlocklist = null)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return true;

        var lower = phrase.Trim().ToLowerInvariant();
        foreach (var blocked in DefaultBlocklist.Concat(extraBlocklist ?? []))
        {
            if (string.IsNullOrWhiteSpace(blocked))
                continue;

            if (lower.Contains(blocked.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static IEnumerable<string> Filter(IEnumerable<string> phrases, IEnumerable<string>? extraBlocklist = null) =>
        phrases
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(p => !IsBlocked(p, extraBlocklist));

    public static bool IsRelevantToKeyword(string keyword, string phrase, IEnumerable<string>? extraBlocklist = null)
    {
        if (string.IsNullOrWhiteSpace(phrase) || IsBlocked(phrase, extraBlocklist))
            return false;

        var trimmed = phrase.Trim();
        var corePhrase = BuildKeywordCorePhrase(keyword);
        if (corePhrase.Length >= 5
            && trimmed.Contains(corePhrase, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tokens = TokenizeKeyword(keyword);
        if (tokens.Count == 0)
            return true;

        var haystack = trimmed.Replace('-', ' ').Replace('_', ' ');
        return tokens.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> FilterForKeyword(
        string keyword,
        IEnumerable<string> phrases,
        IEnumerable<string>? extraBlocklist = null) =>
        Filter(phrases, extraBlocklist).Where(p => IsRelevantToKeyword(keyword, p, extraBlocklist));

    public static string RewritePasfAsQuestion(string phrase, string pillarKeyword)
    {
        var trimmed = phrase.Trim();
        if (trimmed.Contains('?', StringComparison.Ordinal))
            return trimmed;

        return $"What should businesses know about {trimmed} for {pillarKeyword}?";
    }

    private static string BuildKeywordCorePhrase(string keyword)
    {
        var tokens = TokenizeKeyword(keyword);
        return tokens.Count == 0 ? string.Empty : string.Join(' ', tokens);
    }

    private static List<string> TokenizeKeyword(string keyword) =>
        KeywordWordSplit().Split(keyword.ToLowerInvariant())
            .Select(t => t.Trim())
            .Where(t => t.Length > 2 && !KeywordStopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex KeywordWordSplit();
}
