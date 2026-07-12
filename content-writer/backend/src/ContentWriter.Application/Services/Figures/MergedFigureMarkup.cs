using System.Text.RegularExpressions;

namespace ContentWriter.Application.Services.Figures;

/// <summary>
/// Removes legacy inline figure HTML from post bodies. Section images are layout slots, not body markup.
/// </summary>
public static partial class MergedFigureMarkup
{
    [GeneratedRegex(
        @"<figure\s[^>]*data-geek-figure=""1""[^>]*>.*?</figure>\s*",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MergedFigureRegex();

    public static string Strip(string body) => MergedFigureRegex().Replace(body, string.Empty);
}
