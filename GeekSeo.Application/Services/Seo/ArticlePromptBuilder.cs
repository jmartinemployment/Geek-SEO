using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticlePromptBuilder
{
    public static string BuildOutlineSystemPrompt() =>
        $"You are an SEO content strategist. Output a detailed article outline as HTML using h2 and h3 only. " +
        $"Before the closing FAQ, include exactly four body movements in order. " +
        "Each movement must start with <p><strong>Movement {{n}} — {{Phase Label}}</strong></p> followed by one topic-specific <h2>. " +
        "The phase label belongs in the movement line, not in the h2 text. " +
        $"Always end with <h2>{ContentWritingRules.ClosingFaqHeading}</h2> and exactly {ContentWritingRules.ClosingFaqCount} <h3> FAQ questions (no answers in the outline). No preamble. No h1.";

    public static string BuildOutlineUserPrompt(WritingOutlineRequest request)
    {
        var brief = request.Brief;
        var builder = new StringBuilder();
        builder.AppendLine($"Keyword: {request.Keyword}");
        builder.AppendLine($"Target words: {brief.TargetWordCount}");
        builder.AppendLine($"Methodology: {brief.Methodology.Name}");
        builder.AppendLine();
        builder.AppendLine(ArticleMethodologyPrompt.BuildWeaveInstructions(request.Keyword, brief.Methodology));
        builder.AppendLine();
        builder.AppendLine($"Terms to cover: {string.Join(", ", brief.RecommendedTerms)}");
        builder.AppendLine($"Movement heading hints: {string.Join("; ", brief.SuggestedHeadings)}");

        if (brief.DirectAnswerBlocks.Count > 0)
        {
            builder.AppendLine("Direct-answer requirements:");
            foreach (var block in brief.DirectAnswerBlocks)
                builder.AppendLine($"- {block.Label}: {block.Instruction}");
        }

        if (brief.PeopleAlsoAsk.Count > 0)
            builder.AppendLine($"PAA questions: {string.Join("; ", brief.PeopleAlsoAsk)}");
        AppendClosingFaqInstructions(builder, brief);
        if (brief.SerpIntelligence.RelatedSearches.Count > 0)
            builder.AppendLine($"Related searches: {string.Join("; ", brief.SerpIntelligence.RelatedSearches)}");
        if (!string.IsNullOrWhiteSpace(brief.SerpIntelligence.FeaturedSnippet))
            builder.AppendLine($"Featured snippet to outperform: {brief.SerpIntelligence.FeaturedSnippet}");
        if (brief.CompetitorHeadingHighlights.Count > 0)
            builder.AppendLine($"Competitor heading patterns: {string.Join("; ", brief.CompetitorHeadingHighlights)}");
        if (brief.CompetitorSchemaTypes.Count > 0)
            builder.AppendLine($"Competitor schema signals: {string.Join(", ", brief.CompetitorSchemaTypes)}");

        return builder.ToString().Trim();
    }

    public static string BuildDraftSystemPrompt() =>
        $"You write SEO articles in HTML (h1 once, multiple h2/h3, paragraphs). Natural tone. No markdown fences. " +
        "Preserve every <p><strong>Movement N — …</strong></p> label from the outline in the draft. " +
        "Expand each movement with paragraphs that fulfill its intent. Keep the movement label, then the outline h2, then body copy. " +
        $"Always close with <h2>{ContentWritingRules.ClosingFaqHeading}</h2> containing exactly {ContentWritingRules.ClosingFaqCount} topic FAQs as <h3> + <p> pairs.";

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
        builder.AppendLine(ArticleMethodologyPrompt.BuildWeaveInstructions(request.Keyword, brief.Methodology));

        builder.AppendLine();
        builder.AppendLine("Outline (use these h2 headings; expand each movement per its intent):");
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
        if (brief.SerpIntelligence.RelatedSearches.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Related search expansions: {string.Join(", ", brief.SerpIntelligence.RelatedSearches)}");
        }
        if (!string.IsNullOrWhiteSpace(brief.SerpIntelligence.FeaturedSnippet))
        {
            builder.AppendLine();
            builder.AppendLine($"Featured snippet to beat: {brief.SerpIntelligence.FeaturedSnippet}");
        }

        builder.AppendLine();
        builder.AppendLine($"Schema target: JSON-LD {brief.SchemaBlueprint.PrimaryType}");
        if (brief.SchemaBlueprint.AdditionalTypes.Count > 0)
            builder.AppendLine($"Additional schema types: {string.Join(", ", brief.SchemaBlueprint.AdditionalTypes)}");
        if (brief.SchemaBlueprint.SoftwareEntities.Count > 0)
            builder.AppendLine($"Software entities: {string.Join(", ", brief.SchemaBlueprint.SoftwareEntities)}");
        if (brief.CompetitorHeadingHighlights.Count > 0)
            builder.AppendLine($"Competitor heading patterns to learn from: {string.Join("; ", brief.CompetitorHeadingHighlights)}");
        if (brief.CompetitorSchemaTypes.Count > 0)
            builder.AppendLine($"Competitor schema signals observed: {string.Join(", ", brief.CompetitorSchemaTypes)}");

        AppendClosingFaqInstructions(builder, brief);

        if (brief.ReviewChecklist.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Pre-publish review checklist:");
            foreach (var item in brief.ReviewChecklist)
                builder.AppendLine($"- {item}");
        }

        return builder.ToString().Trim();
    }

    private static void AppendClosingFaqInstructions(StringBuilder builder, ContentBrief brief)
    {
        var questions = brief.ClosingFaqQuestions.Count > 0
            ? brief.ClosingFaqQuestions
            : ContentWritingRules.BuildClosingFaqQuestions(brief.Keyword, brief.PeopleAlsoAsk, brief.NicheContext.GapTopics);

        builder.AppendLine();
        builder.AppendLine(
            $"Closing FAQ section (required): end the article with <h2>{ContentWritingRules.ClosingFaqHeading}</h2> " +
            $"followed by exactly {ContentWritingRules.ClosingFaqCount} Q&A pairs. Each question is an <h3>; each answer is a concise <p> (2-4 sentences).");
        builder.AppendLine("Use these questions in order:");
        for (var i = 0; i < questions.Count; i++)
            builder.AppendLine($"{i + 1}. {questions[i]}");
    }
}
