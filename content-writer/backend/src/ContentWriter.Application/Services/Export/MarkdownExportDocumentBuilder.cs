using System.Text;

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
