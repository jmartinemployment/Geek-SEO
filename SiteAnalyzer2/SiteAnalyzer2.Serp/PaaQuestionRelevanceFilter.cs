using System.Text.RegularExpressions;

namespace SiteAnalyzer2.Serp;

/// <summary>
/// Keeps PAA questions on-topic for the pillar keyword and drops resource-seeker / off-intent phrasing.
/// </summary>
public static partial class PaaQuestionRelevanceFilter
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "for", "to", "of", "in", "on", "how", "what", "is", "are",
        "your", "you", "with", "at", "by", "from", "ai", "implement", "implementation",
    };

    private static readonly string[] IntentBlocklist =
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

    public static bool IsRelevantToKeyword(string keyword, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return false;

        if (IsIntentBlocked(question))
            return false;

        var trimmed = question.Trim();
        var corePhrase = BuildCorePhrase(keyword);
        if (corePhrase.Length >= 5
            && trimmed.Contains(corePhrase, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tokens = Tokenize(keyword);
        if (tokens.Count == 0)
            return true;

        var haystack = trimmed.Replace('-', ' ').Replace('_', ' ');
        return tokens.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> Filter(string keyword, IEnumerable<string> questions)
    {
        var relevant = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question))
                continue;

            var trimmed = question.Trim();
            if (!seen.Add(trimmed))
                continue;

            if (IsRelevantToKeyword(keyword, trimmed))
                relevant.Add(trimmed);
        }

        return relevant;
    }

    public static bool IsIntentBlocked(string phrase) =>
        IntentBlocklist.Any(blocked =>
            !string.IsNullOrWhiteSpace(blocked)
            && phrase.Trim().Contains(blocked, StringComparison.OrdinalIgnoreCase));

    private static string BuildCorePhrase(string keyword)
    {
        var tokens = Tokenize(keyword);
        return tokens.Count == 0
            ? string.Empty
            : string.Join(' ', tokens);
    }

    private static List<string> Tokenize(string keyword) =>
        WordSplit().Split(keyword.ToLowerInvariant())
            .Select(t => t.Trim())
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex WordSplit();
}
