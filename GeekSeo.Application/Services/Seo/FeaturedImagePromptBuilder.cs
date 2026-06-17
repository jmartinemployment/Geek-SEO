using System.Text;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public static class FeaturedImagePromptBuilder
{
    public static string BuildForDocument(SeoContentDocument document)
    {
        var keyword = string.IsNullOrWhiteSpace(document.TargetKeyword)
            ? document.Title
            : document.TargetKeyword;
        var topic = string.IsNullOrWhiteSpace(document.Title) || document.Title == "Untitled Document"
            ? keyword
            : document.Title;

        return Build(keyword, topic);
    }

    public static string Build(string keyword, string topic)
    {
        var builder = new StringBuilder();
        builder.Append("Professional editorial hero image for a technical business article about ");
        builder.Append(keyword.Trim());
        builder.Append(". Scene: abstract modern workspace showing ");
        builder.Append(VisualMetaphorFor(keyword));
        builder.Append(". Style: modern flat illustration, soft gradients, cool blue and slate palette, minimal clutter. ");
        builder.Append("Composition: wide horizontal banner, subject weighted left or center, negative space on the right. ");
        builder.Append("Constraints: no text, no letters, no logos, no watermarks, no human faces. ");
        builder.Append("Article title context: ");
        builder.Append(topic.Trim());
        builder.Append('.');
        return builder.ToString();
    }

    private static string VisualMetaphorFor(string keyword)
    {
        var value = keyword.ToLowerInvariant();
        if (value.Contains("bookkeep", StringComparison.Ordinal) ||
            value.Contains("account", StringComparison.Ordinal) ||
            value.Contains("ledger", StringComparison.Ordinal))
        {
            return "organized digital ledger tiles, receipts flowing into structured data blocks, subtle green check accents";
        }

        if (value.Contains("integrat", StringComparison.Ordinal) ||
            value.Contains("zapier", StringComparison.Ordinal) ||
            value.Contains("quickbooks", StringComparison.Ordinal))
        {
            return "connected abstract app blocks linked by glowing sync lines";
        }

        if (value.Contains("automat", StringComparison.Ordinal))
        {
            return "workflow nodes and automation arrows over a clean dashboard";
        }

        return "clean laptop dashboard with structured charts and organized documents";
    }
}
