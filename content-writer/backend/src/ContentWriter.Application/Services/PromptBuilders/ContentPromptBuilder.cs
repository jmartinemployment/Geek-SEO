using System.Text;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;

namespace ContentWriter.Application.Services.PromptBuilders;

public interface IContentPromptBuilder
{
    ChatCompletionRequest BuildArticleMetadataPrompt(ProjectGenerationContext context);
    ChatCompletionRequest BuildArticleSectionPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        string sectionHeading,
        int sectionIndex,
        int totalSections,
        IReadOnlyList<string> fullOutline,
        bool isRegeneration);

    ChatCompletionRequest BuildArticleFaqSectionPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> faqQuestions,
        bool isRegeneration);

    ChatCompletionRequest BuildArticleBodyPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> faqQuestions,
        bool isRegeneration = false);
    ChatCompletionRequest BuildBlogMetadataPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle);
    ChatCompletionRequest BuildBlogSectionPrompt(
        ProjectGenerationContext context,
        ArticleDraft sourceArticle,
        BlogMetadataDraft metadata,
        string sectionHeading,
        int sectionIndex,
        int totalSections);
    ChatCompletionRequest BuildBlogBodyPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle, BlogMetadataDraft metadata);
    ChatCompletionRequest BuildSocialPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle, string platform, string articleUrl);
}

public class ContentPromptBuilder : IContentPromptBuilder
{
    private const string ArticleMetadataJsonContract =
        "{\"title\": string, \"metaDescription\": string (max 160 chars), \"keywords\": string[] (5-10 items), \"sectionOutline\": string[] (5-7 declarative H2 headings — exactly ONE tools section with a descriptive name like \"Top AI Tools for {topic}\" (never a bare \"Tools/Platforms\" label), plus final item: \"People Also Ask\")}";

    private const string SocialJsonContract =
        "{\"text\": string}";

    public ChatCompletionRequest BuildArticleMetadataPrompt(ProjectGenerationContext context)
    {
        var system = new StringBuilder()
            .AppendLine("You are a senior technical content writer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine($"Detected brand tone: {context.DetectedTone}.")
            .AppendLine($"Detected site focus/topics: {context.DetectedFocus}.")
            .AppendLine("Respond with ONLY a single valid JSON object — no markdown fences, no commentary.")
            .AppendLine(ArticleMetadataJsonContract)
            .AppendLine("GOOD sectionOutline example: [\"Overview of Enterprise AI\", \"Implementation Framework\", \"Top AI Platforms and Tools\", \"Measuring ROI\", \"People Also Ask\"]")
            .AppendLine("BAD sectionOutline example: [\"What is AI?\", \"How does it work?\"] — never use questions as main H2s.")
            .ToString();

        var user = ResearchBriefBuilder.Build(context, ResearchBriefPhase.ArticleMetadata,
            $"Plan a comprehensive pillar TechnicalArticle targeting the keyword \"{context.TargetKeyword}\" for {context.PublisherName}. " +
            "Derive sectionOutline from keyword SERP and local pack headings (declarative topics like \"Benefits of X\", not questions). " +
            "REQUIRED: include exactly one tools H2 with a descriptive name (e.g. \"Top AI Tools for Sales Prospecting\") — platforms plus which problems an AI implementer solves. Never use a bare \"Tools/Platforms\" heading. " +
            "Title must NOT be a question and must NOT start with \"How\" — use a definitive statement (e.g. \"AI Prospecting and Lead Intelligence: Implementation Guide\"). " +
            "Meta description: concise factual summary for B2B readers. " +
            "End sectionOutline with exactly one FAQ section titled \"People Also Ask\" — PAA questions are answered there in the body step, not as main H2s. " +
            "Return title, metaDescription, keywords, and sectionOutline only (body is written separately).");

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: 0.5,
            MaxOutputTokens: 1536);
    }

    public ChatCompletionRequest BuildArticleSectionPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        string sectionHeading,
        int sectionIndex,
        int totalSections,
        IReadOnlyList<string> fullOutline,
        bool isRegeneration)
    {
        var outlineContext = string.Join("\n", fullOutline.Select((h, i) => $"{i + 1}. {h}"));
        var isFirst = sectionIndex == 0;
        var isTools = PillarSectionClassifier.IsToolsSection(sectionHeading);

        var system = new StringBuilder()
            .AppendLine("You are a senior technical content writer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine("Write ONE section of a schema.org TechnicalArticle pillar — third person, expert, implementation-focused.")
            .AppendLine("Output ONLY HTML for this section. No Markdown. No JSON. No <html>/<body> tags.")
            .AppendLine(isFirst
                ? "Start with 2-3 introductory <p> paragraphs (context and thesis). Do NOT start with \"How\" or a question. Then <h2> for this section."
                : "Start with <h2> for this section only — no intro paragraphs.")
            .AppendLine("Include 2-3 <h3> subsections with multiple <p> paragraphs and at least one <ul> where appropriate.")
            .AppendLine($"Target {ContentLengthTargets.PillarSectionMinWords}-{ContentLengthTargets.PillarSectionTargetMaxWords} words for this section. Do not write other sections.")
            .ToString();

        if (isTools)
        {
            system += Environment.NewLine + BuildToolsSectionGuidance(context);
        }

        if (isRegeneration)
        {
            system += Environment.NewLine + "REGENERATION: use fresh prose and examples.";
        }

        var user = new StringBuilder()
            .AppendLine(ResearchBriefBuilder.Build(context, ResearchBriefPhase.ArticleSection,
                $"Write section {sectionIndex + 1} of {totalSections}: \"{sectionHeading}\"."))
            .AppendLine()
            .AppendLine($"Article title: {metadata.Title}")
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .AppendLine($"Section to write: {sectionHeading}")
            .AppendLine()
            .AppendLine("Full article outline (for context only — write ONLY the assigned section):")
            .AppendLine(outlineContext)
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: isRegeneration ? 0.72 : 0.65,
            MaxOutputTokens: isTools ? 4096 : 2048);
    }

    public ChatCompletionRequest BuildArticleFaqSectionPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> faqQuestions,
        bool isRegeneration)
    {
        var paaBlock = string.Join("\n", faqQuestions.Select((q, i) => $"  - Q{i + 1}: {q}"));

        var system = new StringBuilder()
            .AppendLine("You are a senior technical content writer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine("Write ONLY the \"People Also Ask\" FAQ section of a TechnicalArticle pillar.")
            .AppendLine("Start with <h2>People Also Ask</h2>. Each question is an <h3> followed by a 2-4 sentence answer <p>.")
            .AppendLine("Direct, factual answers. Third person. No Markdown. No JSON.")
            .ToString();

        if (isRegeneration)
        {
            system += Environment.NewLine + "REGENERATION: use fresh phrasing.";
        }

        var user = new StringBuilder()
            .AppendLine(ResearchBriefBuilder.Build(context, ResearchBriefPhase.ArticleFaq,
                "Write the People Also Ask FAQ section."))
            .AppendLine()
            .AppendLine($"Article title: {metadata.Title}")
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .AppendLine()
            .AppendLine("Questions to answer:")
            .AppendLine(paaBlock)
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: isRegeneration ? 0.7 : 0.6,
            MaxOutputTokens: 3072);
    }

    public ChatCompletionRequest BuildArticleBodyPrompt(
        ProjectGenerationContext context,
        ArticleMetadataDraft metadata,
        IReadOnlyList<string> faqQuestions,
        bool isRegeneration = false)
    {
        var outline = metadata.SectionOutline.Count > 0
            ? string.Join("\n", metadata.SectionOutline.Select((h, i) => $"{i + 1}. {h}"))
            : "(no outline provided — use strong declarative H2 structure)";

        var paaBlock = faqQuestions.Count > 0
            ? string.Join("\n", faqQuestions.Select((q, i) => $"  - Q{i + 1}: {q}"))
            : "  (none provided)";

        var system = new StringBuilder()
            .AppendLine("You are a senior technical content writer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine($"Detected brand tone: {context.DetectedTone}.")
            .AppendLine($"Detected site focus/topics: {context.DetectedFocus}.")
            .AppendLine("Content type: schema.org TechnicalArticle — a deep technical pillar, NOT a BlogPosting or FAQ page.")
            .AppendLine("Write an authoritative pillar article. Tone: third person, expert, implementation-focused.")
            .AppendLine("ANTI-PATTERNS (do NOT do these): first/second person blog voice; short 2-sentence sections; question-mark H2s outside the FAQ section; turning the whole article into Q&A.")
            .AppendLine("REQUIRED STRUCTURE:")
            .AppendLine("  1. Opening: 2-3 <p> paragraphs before the first H2 (context, problem, thesis).")
            .AppendLine("  2. Main H2 sections (from outline, excluding FAQ): each with multiple <h3> subsections, 3+ paragraphs, and at least one <ul> where appropriate.")
            .AppendLine("  3. Final H2 \"People Also Ask\" only: each FAQ as <h3> + 2-4 sentence answer <p>. FAQ must NOT appear earlier in the article.")
            .AppendLine("Ground factual claims in AUTHORITATIVE SOURCES — paraphrase and attribute where appropriate.")
            .AppendLine($"Target at least {ContentLengthTargets.PillarMinWords:N0} words (aim for {ContentLengthTargets.PillarTargetMinWords:N0}-{ContentLengthTargets.PillarTargetMaxWords:N0}). Do not stop early.")
            .AppendLine("Respond with ONLY semantic HTML using <h2>/<h3>/<p>/<ul>/<li> tags. No Markdown. No JSON wrapper.")
            .ToString();

        if (isRegeneration)
        {
            system += Environment.NewLine +
                      "This is a REGENERATION: keep the same outline topics but write substantially new prose, examples, and section openings. Do not reuse prior phrasing.";
        }

        var user = new StringBuilder()
            .AppendLine(ResearchBriefBuilder.Build(context, ResearchBriefPhase.ArticleBody,
                $"Write the full pillar article body for {context.PublisherName}. Target keyword: \"{context.TargetKeyword}\"."))
            .AppendLine()
            .AppendLine($"Title: {metadata.Title}")
            .AppendLine($"Meta description: {metadata.MetaDescription}")
            .AppendLine($"Keywords: {string.Join(", ", metadata.Keywords)}")
            .AppendLine()
            .AppendLine("Section outline (mandatory H2 order — declarative headings except the FAQ section):")
            .AppendLine(outline)
            .AppendLine()
            .AppendLine("People Also Ask questions (answer under the final H2 as H3 + paragraph each):")
            .AppendLine(paaBlock)
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: isRegeneration ? 0.75 : 0.65,
            MaxOutputTokens: 8192);
    }

    private const string BlogMetadataJsonContract =
        "{\"title\": string, \"metaDescription\": string (max 160 chars), \"keywords\": string[] (5-10 items), \"sectionOutline\": string[] (4-5 conversational H2 headings — hooks, numbered angles, or how-to framing; do NOT copy pillar H2s verbatim)}";

    public ChatCompletionRequest BuildBlogMetadataPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle)
    {
        var system = new StringBuilder()
            .AppendLine("You are a content marketer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine($"Detected brand tone: {context.DetectedTone}.")
            .AppendLine("Respond with ONLY a single valid JSON object — no markdown fences, no commentary.")
            .AppendLine(BlogMetadataJsonContract)
            .AppendLine("The blog title MUST be different from the pillar title — use a conversational hook, question, or numbered angle (e.g. \"3 Ways...\", \"Why...\"). Never copy the pillar title verbatim.")
            .ToString();

        var user = new StringBuilder()
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .AppendLine($"Pillar article title (do NOT reuse): {sourceArticle.Title}")
            .AppendLine($"Pillar summary: {sourceArticle.MetaDescription}")
            .AppendLine()
            .AppendLine($"Plan a how-to/listicle companion blog ({ContentLengthTargets.BlogRangeLabel} words) with a distinct title, angle, and 4-5 fresh H2 section headings.")
            .AppendLine("Return title, metaDescription, keywords, and sectionOutline only (body is written separately).")
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: 0.6,
            MaxOutputTokens: 1536);
    }

    public ChatCompletionRequest BuildBlogSectionPrompt(
        ProjectGenerationContext context,
        ArticleDraft sourceArticle,
        BlogMetadataDraft metadata,
        string sectionHeading,
        int sectionIndex,
        int totalSections)
    {
        var outlineContext = string.Join("\n", metadata.SectionOutline.Select((h, i) => $"{i + 1}. {h}"));
        var isFirst = sectionIndex == 0;
        var isLast = sectionIndex == totalSections - 1;

        var system = new StringBuilder()
            .AppendLine("You are a content marketer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine($"Detected brand tone: {context.DetectedTone}.")
            .AppendLine("Write ONE section of a schema.org BlogPosting companion article — conversational, practical, first/second person allowed.")
            .AppendLine("Output ONLY HTML for this section. No Markdown. No JSON. No <html>/<body> tags.")
            .AppendLine(isFirst
                ? "Start with 2-3 introductory <p> paragraphs (hook and context). Then <h2> for this section."
                : "Start with <h2> for this section only — no intro paragraphs.")
            .AppendLine("Include 1-2 <h3> subsections where helpful, multiple <p> paragraphs, and at least one <ul> when listing tips or tools.")
            .AppendLine($"Target {ContentLengthTargets.BlogSectionMinWords}-{ContentLengthTargets.BlogSectionTargetMaxWords} words for this section. Do not write other sections.")
            .AppendLine("Do NOT duplicate the pillar article structure or reuse its H2 headings verbatim.")
            .ToString();

        if (isLast)
        {
            system += Environment.NewLine +
                      $"End with a CTA <p> linking readers to the full technical pillar for implementation depth (use placeholder anchor text only — no href URL). Minimum total blog length across all sections is {ContentLengthTargets.BlogMinWords:N0} words.";
        }

        var user = new StringBuilder()
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .AppendLine($"Pillar title (reference only): {sourceArticle.Title}")
            .AppendLine($"Blog title: {metadata.Title}")
            .AppendLine($"Section to write ({sectionIndex + 1}/{totalSections}): {sectionHeading}")
            .AppendLine()
            .AppendLine("Blog outline (write ONLY the assigned section):")
            .AppendLine(outlineContext)
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: 0.72,
            MaxOutputTokens: 3072);
    }

    public ChatCompletionRequest BuildBlogBodyPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle, BlogMetadataDraft metadata)
    {
        var pillarSections = sourceArticle.SectionOutline.Count > 0
            ? string.Join(", ", sourceArticle.SectionOutline)
            : "see pillar summary";

        var system = new StringBuilder()
            .AppendLine("You are a content marketer for an IT consulting firm that specializes in AI implementation.")
            .AppendLine($"Detected brand tone: {context.DetectedTone}.")
            .AppendLine("Write a conversational blog that teases the pillar — do NOT duplicate the pillar structure or reuse its H2 headings verbatim.")
            .AppendLine("Use fresh H2 headings (3-5 sections). Shorter paragraphs; first/second person allowed.")
            .AppendLine($"Target at least {ContentLengthTargets.BlogMinWords:N0} words (aim for {ContentLengthTargets.BlogRangeLabel}). Do not stop early.")
            .AppendLine("Respond with ONLY semantic HTML using <h2>/<h3>/<p>/<ul>/<li>. No Markdown. No JSON wrapper.")
            .ToString();

        var user = new StringBuilder()
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .AppendLine($"Pillar title (link target — do not reuse as blog title): {sourceArticle.Title}")
            .AppendLine($"Pillar summary: {sourceArticle.MetaDescription}")
            .AppendLine($"Pillar section topics (for reference only — do not copy as H2s): {pillarSections}")
            .AppendLine()
            .AppendLine($"Blog title: {metadata.Title}")
            .AppendLine($"Blog meta description: {metadata.MetaDescription}")
            .AppendLine()
            .AppendLine("Write the blog body. Summarize 2-3 key takeaways, add a practical tip or short story, and end with a CTA to read the full technical article for implementation depth.")
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: 0.7,
            MaxOutputTokens: 6144);
    }

    public ChatCompletionRequest BuildSocialPrompt(ProjectGenerationContext context, ArticleDraft sourceArticle, string platform, string articleUrl)
    {
        var (styleGuidance, lengthGuidance, maxTokens) = platform switch
        {
            "Facebook" => (
                "Casual B2B link-share post: 30-50 words (~40-250 characters). Put the hook in the first line before \"See more\" truncates (~200 chars). 1 emoji max. End with the URL and a light CTA.",
                "Keep under 250 characters total when possible.",
                512),
            "LinkedIn" => (
                "Professional thought-leadership post: 200-300 words. Structure: (1) hook in first 30 words — mobile \"see more\" folds at ~210 chars, (2) context/problem, (3) 1-2 insights from the article, (4) CTA + URL. No emojis or at most one.",
                "Aim for 1,300-1,900 characters. Maximum 3,000 characters.",
                2048),
            _ => ("Professional tone, concise, end with the link.", "Keep concise.", 1024)
        };

        var system = new StringBuilder()
            .AppendLine($"You write {platform} posts for an IT consulting firm that specializes in AI implementation.")
            .AppendLine(styleGuidance)
            .AppendLine(lengthGuidance)
            .AppendLine("Respond with ONLY a single valid JSON object — no markdown fences:")
            .AppendLine(SocialJsonContract)
            .AppendLine("JSON rules: one string value for text. Use \\n for line breaks. Plain URL only — no [text](url) markdown.")
            .ToString();

        var user = new StringBuilder()
            .AppendLine($"Article title: {sourceArticle.Title}")
            .AppendLine($"Article summary: {sourceArticle.MetaDescription}")
            .AppendLine($"Link to include verbatim: {articleUrl}")
            .AppendLine($"Target keyword: {context.TargetKeyword}")
            .ToString();

        return new ChatCompletionRequest(
            Messages: new List<ChatMessage> { new(ChatRole.System, system), new(ChatRole.User, user) },
            Temperature: 0.65,
            MaxOutputTokens: maxTokens);
    }

    private static string BuildToolsSectionGuidance(ProjectGenerationContext context)
    {
        return new StringBuilder()
            .AppendLine("TOOLS SECTION REQUIREMENTS:")
            .AppendLine($"Publisher positioning: {context.ImplementerPositioning}")
            .AppendLine("Cover 4-6 major platforms or tools relevant to the target keyword (only well-known products; do not invent feature names).")
            .AppendLine("For EACH platform use this HTML pattern:")
            .AppendLine("  <h3>{Platform name}</h3>")
            .AppendLine("  <p>Brief overview of what the platform does for this use case.</p>")
            .AppendLine("  <ul><li>2-4 factual capability bullets</li></ul>")
            .AppendLine("  <h4>How an AI implementer helps with {Platform}</h4>")
            .AppendLine("  <p>Yes — an AI implementer can facilitate {Platform} deployments. State which client problems are solved ")
            .AppendLine("  (e.g. faster configuration, data models, integrations, Apex/LWC, Agentforce/agents, governance, training). ")
            .AppendLine("  Tie problems to outcomes: reduced time-to-value, fewer failed pilots, production-ready automation.</p>")
            .AppendLine("Example (Salesforce): AI implementers accelerate Salesforce rollouts via AI-assisted data model design, ")
            .AppendLine("configuration workflows, Apex/LWC development, and autonomous agents such as Agentforce.")
            .AppendLine($"Write from the perspective of {context.PublisherName} as the implementer where natural — without hard-selling.")
            .AppendLine($"Target {ContentLengthTargets.PillarToolsSectionMinWords}-{ContentLengthTargets.PillarToolsSectionTargetMaxWords} words for this Tools section (longer than other sections).")
            .AppendLine("Each platform <h3> should describe a real software product suitable for schema.org SoftwareApplication JSON+LD.")
            .ToString();
    }
}
