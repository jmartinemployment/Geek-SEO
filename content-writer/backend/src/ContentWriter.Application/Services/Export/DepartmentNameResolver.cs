using System.Text;
using System.Text.RegularExpressions;

namespace ContentWriter.Application.Services.Export;

public static partial class DepartmentNameResolver
{
    [GeneratedRegex(@"/use-cases/([^/?#]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UseCasesDepartmentRegex();

    public static string Resolve(
        string? articleUrl,
        string? blogUrl,
        string? projectUrl,
        string? projectName,
        string? departmentOverride)
    {
        if (!string.IsNullOrWhiteSpace(departmentOverride))
            return SanitizeDirectorySegment(departmentOverride);

        foreach (var url in new[] { articleUrl, projectUrl, blogUrl })
        {
            var department = TryExtractFromUrl(url);
            if (department is not null)
                return department;
        }

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            var segment = projectName.Split(['-', ':', '|'], 2, StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrWhiteSpace(segment))
                return SanitizeDirectorySegment(segment);
        }

        return "general";
    }

    private static string? TryExtractFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = UseCasesDepartmentRegex().Match(url);
        if (!match.Success)
            return null;

        var candidate = match.Groups[1].Value;
        return string.IsNullOrWhiteSpace(candidate) ? null : SanitizeDirectorySegment(candidate);
    }

    public static string SanitizeDirectorySegment(string value)
    {
        var sb = new StringBuilder();
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch == '-')
                sb.Append(ch);
            else if (ch is ' ' or '_')
                sb.Append('-');
        }

        var result = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? "general" : result;
    }

    /// <summary>
    /// Human-readable folder name for exports: {department}/{topic}/Pillar|Blog|...
    /// Preserves keyword casing and spaces; strips characters invalid on the local filesystem.
    /// </summary>
    public static string ResolveTopicFolder(string? targetKeyword, string slugFallback)
    {
        var fromKeyword = SanitizeTopicFolderName(targetKeyword);
        if (!string.IsNullOrEmpty(fromKeyword))
            return fromKeyword;

        var fromSlug = SanitizeTopicFolderName(slugFallback.Replace('-', ' '));
        if (!string.IsNullOrEmpty(fromSlug))
            return fromSlug;

        return "general";
    }

    public static string SanitizeTopicFolderName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var invalid = Path.GetInvalidFileNameChars()
            .Concat(['/', '\\', ':'])
            .Distinct()
            .ToArray();
        var sb = new StringBuilder();
        var lastWasSpace = false;

        foreach (var ch in value.Trim())
        {
            if (invalid.Contains(ch))
                continue;

            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            sb.Append(ch);
            lastWasSpace = false;
        }

        return sb.ToString().Trim();
    }
}
