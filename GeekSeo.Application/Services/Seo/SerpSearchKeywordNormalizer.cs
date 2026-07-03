using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Strips Google search operators from saved SERP page titles (e.g. site:wiki in the title bar).
/// </summary>
public static partial class SerpSearchKeywordNormalizer
{
    private static readonly Regex SearchOperator = SearchOperatorRegex();

    public static string Normalize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var stripped = SearchOperator.Replace(query, " ");
        stripped = Regex.Replace(stripped, @"\s+", " ").Trim();
        return stripped;
    }

    public static bool ContainsSearchOperators(string? query) =>
        !string.IsNullOrWhiteSpace(query) && SearchOperator.IsMatch(query);

    [GeneratedRegex(@"\s*(?:site|filetype|intitle|inurl|allintitle|allinurl):[^\s""]+", RegexOptions.IgnoreCase)]
    private static partial Regex SearchOperatorRegex();
}
