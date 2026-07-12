using System.Text.RegularExpressions;
using ContentWriter.Application.Providers;

namespace ContentWriter.Application.Services.Publish;

/// <summary>Validates pillar markdown contract before GeekAPI publish. No mutation.</summary>
internal static class PillarMarkdownValidator
{
    private static readonly string[] LegacyJunkPatterns =
    [
        "Section image:",
        "Accounting Use Case",
        "## Related",
        "## Call to Action",
        "## Frequently Asked Questions",
    ];

    public static void Validate(string markdown, string title, bool requirePeopleAlsoAsk)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new ContentGenerationException("Pillar body is empty.");
        }

        foreach (var pattern in LegacyJunkPatterns)
        {
            if (markdown.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentGenerationException(
                    $"Pillar body contains legacy import artifact '{pattern}'. Regenerate content in Content-Writer.");
            }
        }

        if (markdown.Contains($"# {title}", StringComparison.Ordinal))
        {
            throw new ContentGenerationException(
                "Pillar body contains a duplicate title heading. Regenerate content in Content-Writer.");
        }

        if (!HasIntroProse(markdown))
        {
            throw new ContentGenerationException(
                "Pillar body must include introductory prose before the first ## section.");
        }

        if (requirePeopleAlsoAsk && !ContainsPeopleAlsoAskSection(markdown))
        {
            throw new ContentGenerationException(
                "Pillar body is missing required '## People Also Ask' section.");
        }
    }

    private static bool HasIntroProse(string markdown)
    {
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsPeopleAlsoAskSection(string markdown) =>
        Regex.IsMatch(markdown, @"^##\s+People Also Ask\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
}
