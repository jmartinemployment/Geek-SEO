using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

public static partial class HtmlTextUtility
{
    public static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var withoutTags = HtmlTagRegex().Replace(html, " ");
        return WhitespaceRegex().Replace(withoutTags, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
