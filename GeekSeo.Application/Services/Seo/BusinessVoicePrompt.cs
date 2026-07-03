using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class BusinessVoicePrompt
{
    public static void AppendInstructions(StringBuilder builder, BusinessVoicePack pack)
    {
        if (!pack.Enabled)
            return;

        builder.AppendLine();
        builder.AppendLine("Business voice pack (required — gated before publish):");
        builder.AppendLine(
            $"- Write as {pack.SiteName} speaking to SMB buyers"
            + (string.IsNullOrWhiteSpace(pack.GeoLabel) ? "." : $" in {pack.GeoLabel}.")
            + " No generic national marketing-blog tone.");

        builder.AppendLine(
            $"- Include at least {pack.MinimumConcreteExamples} concrete, named-tool examples "
            + $"(pick from: {string.Join(", ", pack.SuggestedToolExamples)}). "
            + "Show what data feeds the workflow and which system is involved — not abstract \"insights.\"");

        if (pack.RequiresTraditionalVsAiContrast)
        {
            if (pack.RequiresPerSectionContrast)
            {
                builder.AppendLine(
                    "- In each of the four methodology sections, include at least one paired old-way vs. AI-way bullet or sentence "
                    + "(e.g. static whiteboard personas vs. clustering live support transcripts or CRM stages). "
                    + $"The \"{pack.DataQualityPhaseLabel}\" section should carry the strongest contrast.");
            }
            else
            {
                builder.AppendLine(
                    $"- In the \"{pack.DataQualityPhaseLabel}\" section, include an explicit old-way vs. AI-way contrast "
                    + "(e.g. static whiteboard personas vs. clustering live support transcripts or CRM stages).");
            }
        }

        if (pack.RequiresLocalMarketExamples)
        {
            builder.AppendLine(
                $"- Include at least {pack.MinimumLocalMarketExamples} examples grounded in {pack.GeoLabel} SMB workflows "
                + "(e.g. call → booking → CRM follow-up for a local service business — not generic national e-commerce vignettes).");
        }

        if (pack.RequiresCapabilityBridge)
        {
            builder.AppendLine(
                "- In \"Choose the Right AI Technologies\" or \"Implementation Strategy\", add one paragraph on how "
                + $"{pack.SiteName} implements this for clients using: {string.Join(", ", pack.DeclaredCapabilities)}.");
        }

        builder.AppendLine("- Open with two concise direct-answer paragraphs — punchy, not a dense wall of text.");
        builder.AppendLine(
            $"- Each methodology <h2> section needs depth: at least 2–3 paragraphs (~{ResearchDraftWordTarget.MinWordsPerMethodologySection}+ words per section).");
        builder.AppendLine("- Do not invent named experts, credentials, or competitor URLs. Do not add a Sources section (it is appended from research).");
        builder.AppendLine("- Skip resource-download intents (PDFs, templates, generators).");

        if (pack.WritingRecommendations.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Site writing recommendations:");
            foreach (var recommendation in pack.WritingRecommendations)
                builder.AppendLine($"- {recommendation}");
        }

        builder.AppendLine();
        builder.AppendLine("Required CTA (place after the last body section, before Frequently Asked Questions):");
        builder.AppendLine(pack.CtaParagraphHtml);
    }

    public static string BuildRepairSystemPrompt() =>
        "You revise SEO article HTML to satisfy business voice gates. Preserve the existing <h1>, exactly four methodology <h2> body sections, and the closing FAQ. " +
        "Output HTML only. Original prose only — do not copy or closely paraphrase SERP snippets or competitor headings. " +
        "Add missing concrete tool examples, traditional-vs-AI contrast, implementation bridge, and CTA — do not remove factual content. No Sources section. No markdown fences.";

    public static string BuildRepairUserPrompt(
        string html,
        BusinessVoicePack pack,
        IReadOnlyList<BusinessVoiceValidator.GateResult> failedGates)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Keyword: {pack.Keyword}");
        builder.AppendLine($"Site: {pack.SiteName}");
        if (!string.IsNullOrWhiteSpace(pack.GeoLabel))
            builder.AppendLine($"Market: {pack.GeoLabel}");
        builder.AppendLine();
        builder.AppendLine("Failed gates:");
        foreach (var gate in failedGates)
            builder.AppendLine($"- {gate.GateId}: {gate.Detail}");
        builder.AppendLine();
        AppendInstructions(builder, pack);
        builder.AppendLine();
        builder.AppendLine("Article to revise:");
        builder.AppendLine(html.Trim());
        return builder.ToString().TrimEnd();
    }
}
