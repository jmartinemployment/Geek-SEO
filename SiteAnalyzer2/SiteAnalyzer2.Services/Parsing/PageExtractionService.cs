using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Utilities;
using System.Text.Json;

namespace SiteAnalyzer2.Services.Parsing;

public record ExtractedHeading(int Level, string Text, int Sequence);

public record ExtractedMetaTag(string NameOrProperty, string Content);

public record ExtractedJsonLd(string RawJson, string? ParsedType);

public record ExtractedContentBlock(string BlockType, string? Content, int Sequence);

public record ExtractedInternalLink(string Href, string? AnchorText, string AbsoluteUrl);

public record PageExtractionResult(
    string? CanonicalUrl,
    IReadOnlyList<ExtractedHeading> Headings,
    IReadOnlyList<ExtractedMetaTag> MetaTags,
    IReadOnlyList<ExtractedJsonLd> JsonLdBlocks,
    IReadOnlyList<ExtractedContentBlock> ContentBlocks,
    IReadOnlyList<ExtractedInternalLink> InternalLinks);

public class PageExtractionService(AppDbContext db)
{
    private static readonly HtmlParser Parser = new();
    private static readonly HashSet<string> CommercialSchemaTypes =
    [
        "Product", "Service", "LocalBusiness", "Offer", "FAQPage"
    ];

    public PageExtractionResult Extract(string html, Uri pageUrl, string siteRegistrableDomain)
    {
        var document = Parser.ParseDocument(html);
        var sequence = 0;

        var headings = document.QuerySelectorAll("h1, h2, h3, h4, h5, h6")
            .Select(h =>
            {
                var level = int.Parse(h.TagName[1..]);
                return new ExtractedHeading(level, NormalizeHeadingText(h), sequence++);
            })
            .ToList();

        var metaTags = new List<ExtractedMetaTag>();
        foreach (var meta in document.QuerySelectorAll("meta"))
        {
            var name = meta.GetAttribute("name")
                       ?? meta.GetAttribute("property")
                       ?? meta.GetAttribute("http-equiv");
            var content = meta.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(content))
                metaTags.Add(new ExtractedMetaTag(name.Trim(), content.Trim()));
        }

        var canonical = document.QuerySelector("link[rel='canonical']")?.GetAttribute("href");
        var title = document.QuerySelector("title")?.TextContent?.Trim();
        if (!string.IsNullOrWhiteSpace(title))
            metaTags.Add(new ExtractedMetaTag("title", title));

        var jsonLdBlocks = document.QuerySelectorAll("script[type='application/ld+json']")
            .Select(script =>
            {
                var raw = script.TextContent.Trim();
                return new ExtractedJsonLd(raw, TryParseSchemaType(raw));
            })
            .Where(block => !string.IsNullOrWhiteSpace(block.RawJson))
            .ToList();

        var contentBlocks = ExtractContentBlocks(document);

        var internalLinks = ExtractInternalLinks(document, pageUrl, siteRegistrableDomain);

        return new PageExtractionResult(canonical, headings, metaTags, jsonLdBlocks, contentBlocks, internalLinks);
    }

    public async Task RunExtractStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var pages = await db.Pages
            .Where(p => p.RunId == runId && p.HtmlContent != null)
            .ToListAsync(ct);

        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.HtmlContent))
                continue;

            if (!Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUrl))
                continue;

            var domain = page.IsTargetSite ? targetDomain : DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(page.Url));
            var extraction = Extract(page.HtmlContent, pageUrl, domain);

            page.CanonicalUrl = extraction.CanonicalUrl;

            db.PageHeadings.AddRange(extraction.Headings.Select(h => new PageHeading
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                PageId = page.Id,
                Level = h.Level,
                Text = h.Text,
                Sequence = h.Sequence
            }));

            db.PageMetaTags.AddRange(extraction.MetaTags.Select(m => new PageMetaTag
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                PageId = page.Id,
                NameOrProperty = m.NameOrProperty,
                Content = m.Content
            }));

            db.PageJsonLdBlocks.AddRange(extraction.JsonLdBlocks.Select(j => new PageJsonLd
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                PageId = page.Id,
                RawJson = j.RawJson,
                ParsedType = j.ParsedType
            }));

            db.PageContentBlocks.AddRange(extraction.ContentBlocks.Select(b => new PageContentBlock
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                PageId = page.Id,
                BlockType = b.BlockType,
                Content = b.Content,
                Sequence = b.Sequence
            }));
        }

        await db.SaveChangesAsync(ct);
    }

    public static bool HasCommercialSchema(IEnumerable<ExtractedJsonLd> blocks) =>
        blocks.Any(b => b.ParsedType != null && CommercialSchemaTypes.Contains(b.ParsedType));

    public static bool HasCommercialLanguage(string title, string snippet)
    {
        var text = $"{title} {snippet}".ToLowerInvariant();
        string[] signals = ["price", "pricing", "buy", "compare", "comparison", " vs ", "free trial", "demo", "cost", "plans"];
        return signals.Any(text.Contains);
    }

    private static List<ExtractedContentBlock> ExtractContentBlocks(IDocument document)
    {
        var blocks = new List<ExtractedContentBlock>();
        var sequence = 0;

        foreach (var selector in new[] { "main", "article" })
        {
            foreach (var element in document.QuerySelectorAll(selector))
            {
                var text = element.TextContent.Trim();
                if (text.Length > 0)
                    blocks.Add(new ExtractedContentBlock(selector, Truncate(text, 4000), sequence++));
            }
        }

        foreach (var table in document.QuerySelectorAll("table"))
        {
            blocks.Add(new ExtractedContentBlock("table", Truncate(table.TextContent.Trim(), 4000), sequence++));
        }

        foreach (var list in document.QuerySelectorAll("ul, ol"))
        {
            blocks.Add(new ExtractedContentBlock("list", Truncate(list.TextContent.Trim(), 4000), sequence++));
        }

        foreach (var faq in document.QuerySelectorAll("[itemtype*='FAQPage'], details, .faq, [class*='faq']"))
        {
            blocks.Add(new ExtractedContentBlock("faq", Truncate(faq.TextContent.Trim(), 4000), sequence++));
        }

        return blocks;
    }

    private static List<ExtractedInternalLink> ExtractInternalLinks(IDocument document, Uri pageUrl, string siteRegistrableDomain)
    {
        var links = new List<ExtractedInternalLink>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#') || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(pageUrl, href, out var absolute))
                continue;

            if (!string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                continue;

            var linkDomain = DomainHelper.GetRegistrableDomain(absolute.Host);
            if (!string.Equals(linkDomain, siteRegistrableDomain, StringComparison.OrdinalIgnoreCase))
                continue;

            var normalized = NormalizeUrl(absolute);
            if (!seen.Add(normalized))
                continue;

            links.Add(new ExtractedInternalLink(href, anchor.TextContent.Trim(), normalized));
        }

        return links;
    }

    private static string? TryParseSchemaType(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return ExtractSchemaType(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractSchemaType(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("@type", out var typeProp))
                    return ReadTypeValue(typeProp);

                if (element.TryGetProperty("@graph", out var graph))
                {
                    foreach (var item in graph.EnumerateArray())
                    {
                        var graphType = ExtractSchemaType(item);
                        if (graphType != null)
                            return graphType;
                    }
                }

                return null;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var arrayType = ExtractSchemaType(item);
                    if (arrayType != null)
                        return arrayType;
                }

                return null;

            default:
                return null;
        }
    }

    private static string? ReadTypeValue(JsonElement typeProp)
    {
        if (typeProp.ValueKind == JsonValueKind.String)
            return typeProp.GetString();

        if (typeProp.ValueKind == JsonValueKind.Array && typeProp.GetArrayLength() > 0)
        {
            var first = typeProp[0];
            return first.ValueKind == JsonValueKind.String ? first.GetString() : null;
        }

        return null;
    }

    private static string NormalizeHeadingText(IElement heading)
    {
        var parts = new List<string>();
        WalkHeadingNodes(heading, parts);
        return CollapseWhitespace(string.Join(' ', parts));
    }

    private static void WalkHeadingNodes(INode node, List<string> parts)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child)
            {
                case IText textNode:
                {
                    var value = textNode.Data.Trim();
                    if (value.Length > 0)
                        parts.Add(value);
                    break;
                }
                case IElement element when element.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase):
                    break;
                case IElement element:
                    WalkHeadingNodes(element, parts);
                    break;
            }
        }
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string NormalizeUrl(Uri uri) =>
        uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
}
