using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticlePromptBuilder
{
    public static string BuildOutlineSystemPrompt() =>
        "You are an SEO content strategist. Output a detailed article outline as HTML using h2 and h3 only. No preamble.";

    public static string BuildOutlineUserPrompt(WritingOutlineRequest request)
    {
        var brief = request.Brief;
        var builder = new StringBuilder();
        builder.AppendLine($"Keyword: {request.Keyword}");
        builder.AppendLine($"Target words: {brief.TargetWordCount}");
        builder.AppendLine($"Methodology: {brief.Methodology.Name}");
        builder.AppendLine($"Phases: {string.Join(" | ", brief.Methodology.Phases)}");
        builder.AppendLine($"Terms to cover: {string.Join(", ", brief.RecommendedTerms)}");
        builder.AppendLine($"Suggested sections: {string.Join("; ", brief.SuggestedHeadings)}");

        if (brief.DirectAnswerBlocks.Count > 0)
        {
            builder.AppendLine("Direct-answer requirements:");
            foreach (var block in brief.DirectAnswerBlocks)
                builder.AppendLine($"- {block.Label}: {block.Instruction}");
        }

        if (brief.PeopleAlsoAsk.Count > 0)
            builder.AppendLine($"PAA questions: {string.Join("; ", brief.PeopleAlsoAsk)}");

        return builder.ToString().Trim();
    }

    public static string BuildDraftSystemPrompt() =>
        "You write SEO articles in HTML (h1 once, multiple h2/h3, paragraphs). Natural tone. No markdown fences.";

    public static string BuildDraftUserPrompt(WritingDraftRequest request)
    {
        var brief = request.Brief;
        var target = request.TargetWordCount > 0 ? request.TargetWordCount : brief.TargetWordCount;
        var builder = new StringBuilder();

        builder.AppendLine($"Title: {request.Title ?? request.Keyword}");
        builder.AppendLine($"Keyword: {request.Keyword}");
        builder.AppendLine($"Location: {brief.Location}");
        builder.AppendLine($"Target words: {target}");
        builder.AppendLine();
        builder.AppendLine($"Methodology: {brief.Methodology.Name}");
        foreach (var phase in brief.Methodology.Phases)
            builder.AppendLine($"- {phase}");

        builder.AppendLine();
        builder.AppendLine("Outline:");
        builder.AppendLine(request.Outline);
        builder.AppendLine();
        builder.AppendLine($"Include terms: {string.Join(", ", brief.RecommendedTerms.Take(10))}");

        if (brief.DirectAnswerBlocks.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("GEO direct-answer blocks:");
            foreach (var block in brief.DirectAnswerBlocks)
                builder.AppendLine($"- {block.Label}: {block.Instruction}");
        }

        if (brief.TechnicalEvidenceRequirements.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Technical evidence requirements:");
            foreach (var item in brief.TechnicalEvidenceRequirements)
                builder.AppendLine($"- {item}");
        }

        if (brief.GeoAnchorNodes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Geo anchor nodes to reference naturally: {string.Join(", ", brief.GeoAnchorNodes)}");
        }

        builder.AppendLine();
        builder.AppendLine($"Schema target: JSON-LD {brief.SchemaBlueprint.PrimaryType}");
        if (brief.SchemaBlueprint.AdditionalTypes.Count > 0)
            builder.AppendLine($"Additional schema types: {string.Join(", ", brief.SchemaBlueprint.AdditionalTypes)}");
        if (brief.SchemaBlueprint.SoftwareEntities.Count > 0)
            builder.AppendLine($"Software entities: {string.Join(", ", brief.SchemaBlueprint.SoftwareEntities)}");

        if (brief.ReviewChecklist.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Pre-publish review checklist:");
            foreach (var item in brief.ReviewChecklist)
                builder.AppendLine($"- {item}");
        }

        return builder.ToString().Trim();
    }
}
