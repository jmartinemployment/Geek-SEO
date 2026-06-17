using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticlePromptBuilder
{
    public static string BuildOutlineSystemPrompt() =>
        $"You are an SEO content strategist. Output a detailed article outline as HTML using h2 and h3 only. " +
        $"Before the closing FAQ, include exactly four body sections in order — each with one topic-specific <h2>. " +
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
        builder.AppendLine($"Section heading hints: {string.Join("; ", brief.SuggestedHeadings)}");

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
        "Expand each outline section with paragraphs that fulfill its intent. Use only reader-facing headings — no internal labels. " +
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
        builder.AppendLine("Outline (use these h2 headings; expand each section per its intent):");
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

    public static string BuildResearchDraftSystemPrompt() =>
        $"You write SEO articles in HTML (h1 once, multiple h2/h3, paragraphs). Natural tone. No markdown fences. " +
        "Plan sections from the provided section hints and competitor patterns — do not skip hinted phases. " +
        "Use only reader-facing headings. Never output internal labels such as \"Movement 1\", \"Movement 2\", or phase names alone. " +
        $"Always close with <h2>{ContentWritingRules.ClosingFaqHeading}</h2> containing exactly {ContentWritingRules.ClosingFaqCount} topic FAQs as <h3> + <p> pairs.";

    public static string BuildResearchDraftUserPrompt(ResearchDraftRequest request)
    {
        var research = request.Research;
        var keyword = research.DerivedKeyword;
        var target = request.TargetWordCount > 0
            ? request.TargetWordCount
            : Math.Max(800, research.Benchmarks.MedianWordCountTop5);

        var builder = new StringBuilder();
        builder.AppendLine($"Title: {request.Title ?? keyword}");
        builder.AppendLine($"Keyword: {keyword}");
        builder.AppendLine($"Source page: {research.SourceUrl}");
        builder.AppendLine($"Location: {research.SearchLocation}");
        builder.AppendLine($"Target words: {target}");
        builder.AppendLine($"Search intent: {research.IntentPrimary} — {research.IntentJustification}");

        if (!string.IsNullOrWhiteSpace(research.BusinessContext))
            builder.AppendLine($"Business context: {research.BusinessContext}");

        if (!string.IsNullOrWhiteSpace(research.DataQualityNotes))
        {
            builder.AppendLine();
            builder.AppendLine($"Data quality ({research.DataQuality}): {research.DataQualityNotes}");
        }

        if (research.SectionHints.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Section plan (use these h2 headings in order; expand each with paragraphs):");
            foreach (var hint in research.SectionHints.OrderBy(h => h.DisplayOrder))
            {
                builder.Append("- ").Append(hint.SuggestedH2);
                if (hint.SubtopicsFromSerp.Count > 0)
                    builder.Append(" — subtopics: ").Append(string.Join("; ", hint.SubtopicsFromSerp));
                builder.AppendLine();
            }
        }
        else if (research.SourceHeadings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Source page headings to improve upon:");
            foreach (var heading in research.SourceHeadings.OrderBy(h => h.DisplayOrder))
                builder.AppendLine($"- H{heading.Level}: {heading.Text}");
        }

        if (research.RecommendedTerms.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"Include terms: {string.Join(", ", research.RecommendedTerms.Take(12).Select(t => t.Term))}");
        }

        if (!string.IsNullOrWhiteSpace(research.DirectAnswerInstruction))
        {
            builder.AppendLine();
            builder.AppendLine($"GEO direct-answer block: {research.DirectAnswerInstruction}");
        }

        if (!string.IsNullOrWhiteSpace(research.Paf.Text))
        {
            builder.AppendLine();
            builder.AppendLine($"Primary answer feature ({research.Paf.Type}, {research.Paf.Format}): {research.Paf.Text}");
            if (research.MustBeatPaf && !string.IsNullOrWhiteSpace(research.Paf.BeatStrategy))
                builder.AppendLine($"Beat strategy: {research.Paf.BeatStrategy}");
        }

        if (research.PeopleAlsoAsk.Count > 0)
            builder.AppendLine($"PAA questions: {string.Join("; ", research.PeopleAlsoAsk.Take(8).Select(p => p.Question))}");

        if (research.RelatedSearches.Count > 0)
            builder.AppendLine($"Related searches: {string.Join("; ", research.RelatedSearches.Take(8).Select(p => p.SearchText))}");

        var competitorHeadings = research.Competitors
            .SelectMany(c => c.Headings.Where(h => h.Level <= 3).Select(h => h.Text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        if (competitorHeadings.Count > 0)
            builder.AppendLine($"Competitor heading patterns: {string.Join("; ", competitorHeadings)}");

        AppendResearchClosingFaqInstructions(builder, research);

        return builder.ToString().Trim();
    }

    private static void AppendResearchClosingFaqInstructions(StringBuilder builder, WritingResearchContext research)
    {
        var questions = research.ClosingFaqs.Count > 0
            ? research.ClosingFaqs.OrderBy(f => f.DisplayOrder).Select(f => f.Question).ToList()
            : ContentWritingRules.BuildClosingFaqQuestions(
                research.DerivedKeyword,
                research.PeopleAlsoAsk.Select(p => p.Question).ToList(),
                []);

        builder.AppendLine();
        builder.AppendLine(
            $"Closing FAQ section (required): end the article with <h2>{ContentWritingRules.ClosingFaqHeading}</h2> " +
            $"followed by exactly {ContentWritingRules.ClosingFaqCount} Q&A pairs. Each question is an <h3>; each answer is a concise <p> (2-4 sentences).");
        builder.AppendLine("Use these questions in order:");
        for (var i = 0; i < questions.Count; i++)
            builder.AppendLine($"{i + 1}. {questions[i]}");
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
