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
            "Each movement must include a visible label line immediately before its <h2>, using this exact format: " +
            "<p><strong>Movement {n} — {Phase Label}</strong></p>. " +
            "Then add one topic-specific <h2> for the keyword (do not use the corporate phase label as the h2 text).");
        builder.AppendLine();

        for (var i = 0; i < methodology.PhaseDefinitions.Count; i++)
        {
            var phase = methodology.PhaseDefinitions[i];
            builder.AppendLine(
                $"Example: <p><strong>Movement {i + 1} — {phase.Label}</strong></p> then <h2>{{topic-specific heading}}</h2>.");
            builder.AppendLine($"Intent for this movement: {phase.Intent}");
            builder.AppendLine(
                $"Heading families (adapt for \"{keyword}\"): {string.Join(", ", phase.HeadingFamilies)}");
            builder.AppendLine();
        }

        builder.AppendLine(
            "Competitor heading patterns are inspiration only. They must not replace the four methodology movements.");
        return builder.ToString().TrimEnd();
    }

    public static string BuildOutlineRepairSystemPrompt() =>
        "You are an SEO content strategist. Output HTML only: exactly four body sections. " +
        "Each section must start with <p><strong>Movement {n} — {Phase Label}</strong></p> then a topic-specific <h2> and optional <h3> sub-points. " +
        "Use the corporate phase labels in the movement line only, not as the h2 text. " +
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
