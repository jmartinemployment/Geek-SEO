using System.Text.RegularExpressions;
using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Application.Services.SchemaBuilders;

namespace ContentWriter.Application.Services;

/// <summary>Extracts platform names from the pillar Tools section for SoftwareApplication JSON+LD.</summary>
public static class ToolsSectionHtmlParser
{
    private static readonly Regex HeadingPattern = new(
        @"<h([2-4])[^>]*>(.*?)</h\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<SoftwareApplicationDescriptor> ExtractApplications(
        string bodyHtml,
        IReadOnlyList<string> sectionOutline)
    {
        if (string.IsNullOrWhiteSpace(bodyHtml) || sectionOutline.Count == 0)
        {
            return Array.Empty<SoftwareApplicationDescriptor>();
        }

        var toolsHeading = sectionOutline.FirstOrDefault(PillarSectionClassifier.IsToolsSection);
        if (string.IsNullOrWhiteSpace(toolsHeading))
        {
            return Array.Empty<SoftwareApplicationDescriptor>();
        }

        var matches = HeadingPattern.Matches(bodyHtml).Cast<Match>().ToList();
        if (matches.Count == 0)
        {
            return Array.Empty<SoftwareApplicationDescriptor>();
        }

        var toolsIndex = matches.FindIndex(match =>
            int.Parse(match.Groups[1].Value) == 2
            && StripTags(match.Groups[2].Value).Equals(toolsHeading, StringComparison.OrdinalIgnoreCase));

        if (toolsIndex < 0)
        {
            return Array.Empty<SoftwareApplicationDescriptor>();
        }

        var applications = new List<SoftwareApplicationDescriptor>();
        for (var i = toolsIndex + 1; i < matches.Count; i++)
        {
            var level = int.Parse(matches[i].Groups[1].Value);
            if (level == 2)
            {
                break;
            }

            if (level != 3)
            {
                continue;
            }

            var name = StripTags(matches[i].Groups[2].Value);
            if (string.IsNullOrWhiteSpace(name)
                || name.StartsWith("How an AI implementer", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var description = ExtractFollowingParagraph(bodyHtml, matches[i].Index + matches[i].Length);
            applications.Add(new SoftwareApplicationDescriptor(name, description));
        }

        return applications;
    }

    private static string? ExtractFollowingParagraph(string html, int startIndex)
    {
        var slice = html[startIndex..];
        var match = Regex.Match(slice, @"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return null;
        }

        var text = StripTags(match.Groups[1].Value);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", " ").Replace("&nbsp;", " ").Trim();
}
