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
        builder.AppendLine(
            $"Depth: each body section should be ~{ResearchDraftWordTarget.MinWordsPerMethodologySection}+ words (multiple paragraphs) — not thin summaries.");
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

    public static string BuildDraftRepairSystemPrompt(WritingMethodologySpec methodology)
    {
        var sectionCount = methodology.PhaseDefinitions.Count;
        return
            $"You restructure SEO article HTML. Output HTML only with the same <h1> if present, exactly {sectionCount} body <h2> sections in methodology order, optional <h3> subtopics, and paragraphs preserved or lightly edited. " +
            "Never add extra body <h2> sections. Map competitor-only topics to <h3> under the closest methodology section. " +
            $"Keep or restore <h2>{ContentWritingRules.ClosingFaqHeading}</h2> when it was in the input. No markdown fences.";
    }

    public static string BuildDraftRepairUserPrompt(
        string keyword,
        WritingMethodologySpec methodology,
        string draftHtml,
        IReadOnlyList<WritingResearchSectionHint> sectionHints,
        IReadOnlyList<string> competitorHeadingHighlights)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Keyword: {keyword}");
        builder.AppendLine();
        builder.AppendLine(BuildWeaveInstructions(keyword, methodology));

        if (sectionHints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Required section plan:");
            foreach (var hint in sectionHints.OrderBy(h => h.DisplayOrder))
            {
                builder.Append("- ").Append(hint.Label).Append(": <h2>").Append(hint.SuggestedH2).Append("</h2>");
                if (hint.SubtopicsFromSerp.Count > 0)
                    builder.Append(" — subtopics as <h3>: ").Append(string.Join("; ", hint.SubtopicsFromSerp));
                builder.AppendLine();
            }
        }

        if (competitorHeadingHighlights.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(
                $"Competitor headings (use as <h3> only, never as extra <h2>): {string.Join("; ", competitorHeadingHighlights)}");
        }

        builder.AppendLine();
        builder.AppendLine("Article to restructure:");
        builder.AppendLine(draftHtml.Trim());
        builder.AppendLine();
        builder.AppendLine(
            $"Reorganize into exactly {methodology.PhaseDefinitions.Count} methodology-aligned body <h2> sections. Preserve factual paragraphs under the best-matching section.");
        return builder.ToString().TrimEnd();
    }
}
