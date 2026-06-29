using System.Text;

namespace GeekSeo.Application.Services.Seo;

public static class ContentBlogSpokePromptBuilder
{
    public static string BuildSystemPrompt() =>
        "You are an SEO content writer. Output HTML article body only (h2/h3/p, no h1). " +
        "800-1200 words. Different search intent than the pillar — not a longer-tail modifier of the pillar keyword. " +
        "End with <h2>Frequently Asked Questions</h2> and exactly 3 FAQ h3+p pairs.";

    public static string BuildUserPrompt(
        string pillarTitle,
        string pillarKeyword,
        string spokeType,
        string? spokeKeyword,
        string pillarHtml)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Pillar title: {pillarTitle}");
        builder.AppendLine($"Pillar primary keyword (do NOT target this): {pillarKeyword}");
        builder.AppendLine($"Spoke type: {spokeType}");
        if (!string.IsNullOrWhiteSpace(spokeKeyword))
            builder.AppendLine($"Spoke primary keyword (target this): {spokeKeyword}");
        else
            builder.AppendLine("Choose a distinct spoke primary keyword and use it naturally in the article.");
        builder.AppendLine();
        builder.AppendLine("Pillar article for context:");
        builder.AppendLine(TruncatePlain(pillarHtml, 8000));
        return builder.ToString();
    }

    public static string BuildMetadataSystemPrompt() =>
        "Reply ONLY with JSON: {\"title\":\"...\",\"slug\":\"kebab-case\",\"primaryKeyword\":\"...\",\"excerpt\":\"...\",\"metaDescription\":\"...\"}. " +
        "slug must be lowercase kebab-case. excerpt ~120 chars. metaDescription 150-160 chars.";

    public static string BuildMetadataUserPrompt(
        string spokeType,
        string pillarKeyword,
        string spokeHtml) =>
        $"Spoke type: {spokeType}\nPillar keyword (avoid): {pillarKeyword}\n\nSpoke HTML:\n{TruncatePlain(spokeHtml, 6000)}";

    private static string TruncatePlain(string html, int maxChars)
    {
        var plain = StripHtml(html);
        return plain.Length <= maxChars ? plain : plain[..maxChars];
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ", StringComparison.Ordinal)
            .Trim();
    }
}
