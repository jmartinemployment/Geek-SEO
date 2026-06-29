using System.Text.RegularExpressions;

namespace SiteAnalyzer2.Services.Utilities;

public static partial class KeywordPathMatcher
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "for", "to", "of", "in", "on", "how", "what", "is", "are",
        "your", "you", "with", "at", "by", "from", "ai", "implement", "implementation",
    };

    /// <summary>exact = path slug mirrors keyword; strong = 2+ topic tokens; weak = 1 token.</summary>
    public static string? Score(string keyword, string url)
    {
        if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();
        if (path.Length == 0)
            return null;

        var slug = path.Split('/').LastOrDefault() ?? path;
        slug = slug.Replace('-', ' ').Replace('_', ' ');

        var tokens = Tokenize(keyword);
        if (tokens.Count == 0)
            return null;

        var pathText = $"{path} {slug}".Replace('-', ' ').Replace('_', ' ');
        var hits = tokens.Count(t => pathText.Contains(t, StringComparison.OrdinalIgnoreCase));
        if (hits == 0)
            return null;

        var slugCompact = RegexReplaceNonAlnum().Replace(slug, "");
        var keywordCompact = RegexReplaceNonAlnum().Replace(string.Join(' ', tokens), "");
        if (slugCompact.Length > 0 && keywordCompact.Contains(slugCompact, StringComparison.OrdinalIgnoreCase))
            return "exact";

        return hits >= 2 ? "strong" : "weak";
    }

    /// <summary>True when URL, title, or snippet contains at least one significant keyword token.</summary>
    public static bool ContainsAnyKeywordToken(string keyword, string? url, string? title = null, string? snippet = null)
    {
        var tokens = Tokenize(keyword);
        if (tokens.Count == 0)
            return true;

        var haystack = string.Join(' ', url ?? "", title ?? "", snippet ?? "")
            .Replace('-', ' ')
            .Replace('_', ' ');

        return tokens.Any(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> Tokenize(string keyword)
    {
        return WordSplit().Split(keyword.ToLowerInvariant())
            .Select(t => t.Trim())
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex RegexReplaceNonAlnum();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex WordSplit();
}
