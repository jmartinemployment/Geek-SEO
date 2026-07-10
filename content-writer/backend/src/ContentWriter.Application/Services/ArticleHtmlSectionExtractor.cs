using System.Text.RegularExpressions;

namespace ContentWriter.Application.Services;

public static class ArticleHtmlSectionExtractor
{
    private static readonly Regex H2Regex = new(
        @"<h2[^>]*>(.*?)</h2>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractH2Headings(string? bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(bodyHtml))
            return [];

        return H2Regex.Matches(bodyHtml)
            .Select(m => StripTags(m.Groups[1].Value).Trim())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();
    }

    public static IReadOnlyList<ImagePromptSectionTarget> BuildSectionTargets(
        string? pillarBodyHtml,
        string? blogBodyHtml)
    {
        var targets = new List<ImagePromptSectionTarget>();
        var order = 1;

        foreach (var heading in ExtractH2Headings(pillarBodyHtml))
        {
            targets.Add(new ImagePromptSectionTarget("pillar", heading, order++));
        }

        order = 1;
        foreach (var heading in ExtractH2Headings(blogBodyHtml))
        {
            targets.Add(new ImagePromptSectionTarget("blog", heading, order++));
        }

        return targets;
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", " ").Trim();
}

public sealed record ImagePromptSectionTarget(string SourceType, string Heading, int Order);
