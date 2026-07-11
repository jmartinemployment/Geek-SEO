using System.Text;
using System.Text.RegularExpressions;
using ContentWriter.Application.Services.Publish;

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
        string? targetKeyword,
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
            {
                var fromName = SanitizeDirectorySegment(segment);
                if (SiteDepartments.IsKnown(fromName))
                    return SiteDepartments.Normalize(fromName);
            }
        }

        var inferred = InferFromKeyword(targetKeyword);
        if (inferred is not null)
            return inferred;

        return "sales";
    }

    public static string? InferFromKeyword(string? targetKeyword)
    {
        if (string.IsNullOrWhiteSpace(targetKeyword))
            return null;

        var text = targetKeyword.ToLowerInvariant();

        if (text.Contains("account") || text.Contains("finance") || text.Contains("financial")
            || text.Contains("invoice") || text.Contains("bookkeep") || text.Contains("ledger")
            || text.Contains("payable"))
        {
            return "accounting";
        }

        if (text.Contains("customer") || text.Contains("support") || text.Contains("service desk")
            || text.Contains("helpdesk") || text.Contains("ticket"))
        {
            return "customer-service";
        }

        if (text.Contains("human resource") || text.Contains(" hr ") || text.StartsWith("hr ")
            || text.Contains("employee") || text.Contains("hiring") || text.Contains("recruit"))
        {
            return "human-resources";
        }

        if (text.Contains("marketing") || text.Contains("seo") || text.Contains("content gen")
            || text.Contains("campaign") || text.Contains("brand"))
        {
            return "marketing";
        }

        if (text.Contains("sales") || text.Contains("prospect") || text.Contains("lead")
            || text.Contains("pipeline") || text.Contains("crm"))
        {
            return "sales";
        }

        return null;
    }

    private static string? TryExtractFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = UseCasesDepartmentRegex().Match(url);
        if (!match.Success)
            return null;

        var candidate = match.Groups[1].Value;
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        var sanitized = SanitizeDirectorySegment(candidate);
        return SiteDepartments.IsKnown(sanitized) ? SiteDepartments.Normalize(sanitized) : null;
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
        return string.IsNullOrEmpty(result) ? "sales" : result;
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
