using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticleMethodologyPrompt
{
    public static string BuildWeaveInstructions(string keyword, WritingMethodologySpec methodology)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Methodology weave (required body structure):");
        builder.AppendLine(
            $"Write the article body as {methodology.PhaseDefinitions.Count} movements in the order below. " +
            "Each movement gets one topic-specific <h2> for this keyword. " +
            "Do NOT copy the corporate phase labels as H2 text unless they read naturally for this topic.");
        builder.AppendLine();

        for (var i = 0; i < methodology.PhaseDefinitions.Count; i++)
        {
            var phase = methodology.PhaseDefinitions[i];
            builder.AppendLine($"Movement {i + 1} — {phase.Label} (id: {phase.Id})");
            builder.AppendLine($"Intent: {phase.Intent}");
            builder.AppendLine(
                $"Heading families (adapt for \"{keyword}\"): {string.Join(", ", phase.HeadingFamilies)}");
            builder.AppendLine(
                $"Place <!-- methodology:{phase.Id} --> immediately before this movement's <h2>.");
            builder.AppendLine();
        }

        builder.AppendLine(
            "Competitor heading patterns are inspiration only. They must not replace the four methodology movements.");
        return builder.ToString().TrimEnd();
    }

    public static string BuildOutlineRepairSystemPrompt() =>
        "You are an SEO content strategist. Output HTML only: exactly four body sections as h2 + optional h3 sub-points. " +
        "Each section must include <!-- methodology:{phase-id} --> immediately before its h2. " +
        "Use topic-specific h2 text for the keyword. Do not copy corporate phase labels verbatim unless natural. " +
        "No FAQ section. No preamble. No h1. No markdown fences.";

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
        builder.AppendLine("Output only the four methodology body sections in order.");
        return builder.ToString().TrimEnd();
    }
}
