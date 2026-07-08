using System.Text.RegularExpressions;

namespace ContentImageSpike.Infrastructure;

internal static partial class HtmlTextHelper
{
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    public static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = HtmlTagRegex().Replace(html, " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    public static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength].TrimEnd() + "…";
    }
}
