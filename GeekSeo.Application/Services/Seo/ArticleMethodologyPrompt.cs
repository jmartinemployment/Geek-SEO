using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticleMethodologyPrompt
{
    public static string BuildWeaveInstructions(string keyword, WritingMethodologySpec methodology)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Article body structure (required):");
        builder.AppendLine(
            $"Write exactly {methodology.PhaseDefinitions.Count} body sections in the order below. " +
            "Each section is one topic-specific <h2> heading followed by optional <h3> sub-points. " +
            "Do not use internal labels like \"Movement 1\" — only reader-facing headings.");
        builder.AppendLine();

        for (var i = 0; i < methodology.PhaseDefinitions.Count; i++)
        {
            var phase = methodology.PhaseDefinitions[i];
            builder.AppendLine($"Section {i + 1} intent: {phase.Intent}");
            builder.AppendLine(
                $"Heading families (adapt for \"{keyword}\"): {string.Join(", ", phase.HeadingFamilies)}");
            builder.AppendLine();
        }

        builder.AppendLine(
            $"Competitor heading patterns are inspiration only. They must not replace the {methodology.PhaseDefinitions.Count} required body sections.");
        return builder.ToString().TrimEnd();
    }

    public static string BuildOutlineRepairSystemPrompt(WritingMethodologySpec methodology)
    {
        var sectionCount = methodology.PhaseDefinitions.Count;
        return
            $"You are an SEO content strategist. Output HTML only: exactly {sectionCount} body sections. " +
            "Each section is one topic-specific <h2> and optional <h3> sub-points. " +
            "No internal movement labels. No FAQ section. No preamble. No h1. No markdown fences.";
    }

    public static string BuildOutlineRepairUserPrompt(
        string keyword,
        WritingMethodologySpec methodology,
        string? existingOutline,
        IReadOnlyList<string> competitorHeadingHighlights)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Keyword: {keyword}");
        builder.AppendLine();
        builder.AppendLine(BuildWeaveInstructions(keyword, methodology));

        if (!string.IsNullOrWhiteSpace(existingOutline))
        {
            builder.AppendLine();
            builder.AppendLine("Existing outline (replace weak body sections; keep useful h3 ideas):");
            builder.AppendLine(existingOutline.Trim());
        }

        if (competitorHeadingHighlights.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"SERP heading patterns to learn from: {string.Join("; ", competitorHeadingHighlights)}");
        }

        builder.AppendLine();
        builder.AppendLine($"Output only the {methodology.PhaseDefinitions.Count} body sections in order.");
        return builder.ToString().TrimEnd();
    }
}
