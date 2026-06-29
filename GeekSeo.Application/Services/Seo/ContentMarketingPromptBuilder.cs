using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentMarketingPromptBuilder
{
    public static string BuildSummariesSystemPrompt() =>
        "You write marketing copy for a B2B AI services website. " +
        "Reply ONLY with JSON: {\"homeSummary\":\"...\",\"hubSummary\":\"...\",\"metaDescription\":\"...\"}. " +
        "homeSummary: ~100-130 chars, outcome/stat led for homepage cards. " +
        "hubSummary: ~120-160 chars, definitional for department hub cards. " +
        "metaDescription: 150-160 chars for HTML meta. " +
        "All three strings must be pairwise distinct wording.";

    public static string BuildSummariesUserPrompt(string title, string keyword, string pillarHtml)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Use case title: {title}");
        builder.AppendLine($"Primary keyword: {keyword}");
        builder.AppendLine();
        builder.AppendLine("Pillar article HTML:");
        builder.AppendLine(TruncatePlain(pillarHtml, 12000));
        return builder.ToString();
    }

    public static string BuildBlogSpokeSystemPrompt() =>
        "You are an SEO content writer. Output HTML article body only (h2/h3/p, no h1). " +
        "800-1200 words. Different search intent than the pillar — not a longer-tail modifier of the pillar keyword. " +
        "End with <h2>Frequently Asked Questions</h2> and exactly 3 FAQ h3+p pairs.";

    public static string BuildBlogSpokeUserPrompt(
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

    public static string BuildBlogSpokeMetadataSystemPrompt() =>
        "Reply ONLY with JSON: {\"title\":\"...\",\"slug\":\"kebab-case\",\"primaryKeyword\":\"...\",\"excerpt\":\"...\",\"metaDescription\":\"...\"}. " +
        "slug must be lowercase kebab-case. excerpt ~120 chars. metaDescription 150-160 chars.";

    public static string BuildBlogSpokeMetadataUserPrompt(
        string spokeType,
        string pillarKeyword,
        string spokeHtml) =>
        $"Spoke type: {spokeType}\nPillar keyword (avoid): {pillarKeyword}\n\nSpoke HTML:\n{TruncatePlain(spokeHtml, 6000)}";

    public static string BuildSocialSystemPrompt() =>
        "You write social distribution copy for Geek at Your Spot (South Florida B2B AI). " +
        "Reply ONLY with JSON: {\"linkedin\":{\"body\":\"...\",\"linkTargetKind\":\"pillar\",\"linkTargetSlug\":\"...\"}," +
        "\"facebook\":{\"body\":\"...\",\"linkTargetKind\":\"blog\",\"linkTargetSlug\":\"...\"}}. " +
        "LinkedIn: ~1300 chars, professional hook→insight→CTA, 3-5 hashtags at end. " +
        "Facebook: 80-150 words, casual local SMB angle, 0-2 hashtags. " +
        "Bodies must be completely different strings.";

    public static string BuildSocialUserPrompt(
        ContentMarketingBundle bundle,
        string pillarTitle,
        string pillarHtml)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Pillar title: {pillarTitle}");
        builder.AppendLine($"Pillar keyword: {bundle.PrimaryKeyword}");
        builder.AppendLine($"Use case slug (pillar link target): {bundle.UseCaseSlug}");
        if (bundle.BlogSpoke is not null)
        {
            builder.AppendLine($"Blog spoke title: {bundle.BlogSpoke.Title}");
            builder.AppendLine($"Blog spoke slug (facebook link target): {bundle.BlogSpoke.Slug}");
        }
        builder.AppendLine();
        builder.AppendLine("Pillar excerpt:");
        builder.AppendLine(TruncatePlain(pillarHtml, 4000));
        if (bundle.BlogSpoke?.ContentHtml is { Length: > 0 } spokeHtml)
        {
            builder.AppendLine();
            builder.AppendLine("Blog spoke excerpt:");
            builder.AppendLine(TruncatePlain(spokeHtml, 3000));
        }
        return builder.ToString();
    }

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
