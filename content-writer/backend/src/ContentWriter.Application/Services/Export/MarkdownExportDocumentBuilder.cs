using System.Text;
using ContentWriter.Application.DTOs;

namespace ContentWriter.Application.Services.Export;

public static class MarkdownExportDocumentBuilder
{
    public static string Build(MarkdownExportInput input)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {YamlScalar(input.Title)}");
        sb.AppendLine($"slug: {YamlScalar(input.Slug)}");
        sb.AppendLine($"metaDescription: {YamlScalar(input.MetaDescription)}");
        sb.AppendLine($"canonicalUrl: {YamlScalar(input.CanonicalUrl)}");
        sb.AppendLine($"contentType: {YamlScalar(input.ContentType)}");
        sb.AppendLine($"department: {YamlScalar(input.Department)}");
        sb.AppendLine($"wordCount: {input.WordCount}");
        sb.AppendLine("keywords:");
        foreach (var keyword in input.Keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
            sb.AppendLine($"  - {YamlScalar(keyword)}");

        if (!string.IsNullOrWhiteSpace(input.RelatedUrl))
            sb.AppendLine($"relatedUrl: {YamlScalar(input.RelatedUrl)}");

        sb.AppendLine($"exportedAtUtc: {YamlScalar(input.ExportedAtUtc.ToString("O"))}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(input.BodyHtml.Trim());
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## JSON-LD Schema");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(input.JsonLdSchema))
        {
            sb.AppendLine("```json");
            sb.AppendLine(input.JsonLdSchema.Trim());
            sb.AppendLine("```");
        }
        else
        {
            sb.AppendLine("_No JSON-LD schema was stored for this content._");
        }

        return sb.ToString();
    }

    public static string BuildSocial(SocialPostDraft post, string department, string slug, DateTime exportedAtUtc)
    {
        var wordCount = CountWords(post.Text);
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"platform: {YamlScalar(post.Platform)}");
        sb.AppendLine($"contentType: social");
        sb.AppendLine($"department: {YamlScalar(department)}");
        sb.AppendLine($"slug: {YamlScalar(slug)}");
        sb.AppendLine($"wordCount: {wordCount}");
        sb.AppendLine($"exportedAtUtc: {YamlScalar(exportedAtUtc.ToString("O"))}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(post.Text.Trim());
        return sb.ToString();
    }

    public static string BuildColdOutreach(ColdOutreachEmailContent email, string department, string slug, DateTime exportedAtUtc)
    {
        var wordCount = CountWords(email.BodyText);
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"subject: {YamlScalar(email.Subject)}");
        sb.AppendLine($"contentType: email-cold-outreach");
        sb.AppendLine($"department: {YamlScalar(department)}");
        sb.AppendLine($"slug: {YamlScalar(slug)}");
        sb.AppendLine($"ctaLabel: {YamlScalar(email.CtaLabel)}");
        sb.AppendLine($"ctaUrl: {YamlScalar(email.CtaUrl)}");
        sb.AppendLine($"wordCount: {wordCount}");
        sb.AppendLine($"exportedAtUtc: {YamlScalar(exportedAtUtc.ToString("O"))}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(email.BodyText.Trim());
        return sb.ToString();
    }

    public static string BuildSectionImagePrompt(
        ImagePromptSectionContent prompt,
        string department,
        string slug,
        DateTime exportedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"sourceType: {YamlScalar(prompt.SourceType)}");
        sb.AppendLine($"heading: {YamlScalar(prompt.Heading)}");
        sb.AppendLine($"order: {prompt.Order}");
        sb.AppendLine($"contentType: image-prompt-section");
        sb.AppendLine($"department: {YamlScalar(department)}");
        sb.AppendLine($"slug: {YamlScalar(slug)}");
        sb.AppendLine($"width: {prompt.Width}");
        sb.AppendLine($"height: {prompt.Height}");
        sb.AppendLine($"leonardoModel: {YamlScalar(prompt.LeonardoModel)}");
        sb.AppendLine($"leonardoModelId: {YamlScalar(prompt.LeonardoModelId)}");
        sb.AppendLine($"stylePreset: {YamlScalar(prompt.StylePreset)}");
        sb.AppendLine($"alchemy: {prompt.Alchemy.ToString().ToLowerInvariant()}");
        sb.AppendLine($"photoReal: {prompt.PhotoReal.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(prompt.Notes))
            sb.AppendLine($"notes: {YamlScalar(prompt.Notes)}");
        sb.AppendLine($"exportedAtUtc: {YamlScalar(exportedAtUtc.ToString("O"))}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(prompt.Prompt.Trim());
        return sb.ToString();
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string YamlScalar(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var needsQuotes = value.Contains(':')
            || value.Contains('#')
            || value.Contains('"')
            || value.Contains('\'')
            || value.StartsWith(' ')
            || value.EndsWith(' ')
            || value.Contains('\n')
            || value.Contains('\r');

        if (!needsQuotes)
            return value;

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }
}

public sealed record MarkdownExportInput(
    string Title,
    string Slug,
    string MetaDescription,
    string CanonicalUrl,
    string ContentType,
    string Department,
    int WordCount,
    IReadOnlyList<string> Keywords,
    string? RelatedUrl,
    string BodyHtml,
    string? JsonLdSchema,
    DateTime ExportedAtUtc);
