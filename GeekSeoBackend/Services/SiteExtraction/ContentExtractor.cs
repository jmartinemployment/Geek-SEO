using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Extracts structured content (tools, FAQs, etc.) from markdown.
/// Extensible for future content types beyond tools.
/// </summary>
public sealed class ContentExtractor
{
    /// <summary>
    /// Extracts tools from PageContext markdown.
    /// Tools are markdown links grouped under headings (departments).
    /// </summary>
    public static List<ExtractedTool> ExtractTools(PageContext context)
    {
        if (string.IsNullOrWhiteSpace(context?.MainContentMarkdown))
            return [];

        var tools = new List<ExtractedTool>();
        var lines = context.MainContentMarkdown.Split(['\r', '\n'], StringSplitOptions.None);
        var currentDepartment = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Track current heading/department
            if (trimmed.StartsWith("#"))
            {
                currentDepartment = trimmed.TrimStart('#').Trim();
                continue;
            }

            // Match markdown link: "- [Name](url)"
            if (trimmed.StartsWith("- ["))
            {
                var toolData = ParseToolLink(trimmed);
                if (toolData is not null)
                {
                    tools.Add(new ExtractedTool(
                        toolData.Value.Name,
                        toolData.Value.Href,
                        currentDepartment,
                        "")); // body/content extracted from context later if needed
                }
            }
        }

        return tools;
    }

    /// <summary>
    /// Computes hash of ContextJson for change detection.
    /// Used by search-engine pattern (skip extraction if unchanged).
    /// </summary>
    public static string ComputeContextHash(string contextJson)
    {
        if (string.IsNullOrWhiteSpace(contextJson))
            return CrawlDocumentHasher.Sha256Hex("");

        return CrawlDocumentHasher.Sha256Hex(contextJson);
    }

    /// <summary>
    /// Parses markdown link: "- [Name](url)" → (Name, url)
    /// </summary>
    private static (string Name, string Href)? ParseToolLink(string line)
    {
        // Format: "- [Name](url)"
        var bracketStart = line.IndexOf('[');
        var bracketEnd = line.IndexOf(']');
        var parenStart = line.IndexOf('(');
        var parenEnd = line.IndexOf(')');

        if (bracketStart < 0 || bracketEnd < 0 || parenStart < 0 || parenEnd < 0)
            return null;

        var name = line[(bracketStart + 1)..bracketEnd].Trim();
        var href = line[(parenStart + 1)..parenEnd].Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href))
            return null;

        return (name, href);
    }
}

/// <summary>
/// Extracted tool data.
/// Uniqueness: (Name, Department, Body) allows same tool in different contexts.
/// </summary>
public sealed record ExtractedTool(
    string Name,
    string Href,
    string Department,
    string Body = "");
