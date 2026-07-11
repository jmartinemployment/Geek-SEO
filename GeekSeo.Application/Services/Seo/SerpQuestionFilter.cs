namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Filters resource-seeker and off-intent SERP questions (PDF, template, generator, etc.)
/// from FAQs, cluster spokes, and prompts.
/// </summary>
public static class SerpQuestionFilter
{
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

    public static string RewritePasfAsQuestion(string phrase, string pillarKeyword)
    {
        var trimmed = phrase.Trim();
        if (trimmed.Contains('?', StringComparison.Ordinal))
            return trimmed;

        return $"What should businesses know about {trimmed} for {pillarKeyword}?";
    }
}
