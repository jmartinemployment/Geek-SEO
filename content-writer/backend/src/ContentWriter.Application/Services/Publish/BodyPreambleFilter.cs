using System.Text.RegularExpressions;
using ContentWriter.Application.Providers;

namespace ContentWriter.Application.Services.Publish;

/// <summary>
/// Removes introductory prose before the first H2. Hero and deck copy live in presentation excerpt fields, not the body.
/// </summary>
public static partial class BodyPreambleFilter
{
    public static string StripPreambleFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var match = FirstH2Regex().Match(html);
        if (!match.Success)
        {
            return html.Trim();
        }

        return html[match.Index..].Trim();
    }

    public static void ValidateMarkdownStartsWithSection(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return;
        }

        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                throw new ContentGenerationException(
                    "Body must start at the first ## section with no introductory prose before it. Regenerate content in Content-Writer.");
            }
        }
    }

    [GeneratedRegex(@"<h2\b", RegexOptions.IgnoreCase)]
    private static partial Regex FirstH2Regex();
}
