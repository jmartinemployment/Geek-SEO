namespace ContentWriter.Application.Services;

/// <summary>Recommended word-count ranges and editorial definitions by content type.</summary>
public static class ContentLengthTargets
{
    // TechnicalArticle pillar / cornerstone pages — broad topical authority, complete guides.
    public const int PillarMinWords = 3_000;
    public const int PillarTargetMinWords = 3_000;
    public const int PillarSectionMinWords = 350;
    public const int PillarSectionTargetMaxWords = 550;
    public const int PillarToolsSectionMinWords = 450;
    public const int PillarToolsSectionTargetMaxWords = 700;

    public const string PillarEditorialDefinition =
        "Pillar and cornerstone TechnicalArticles cover broad topics with exhaustive depth — suitable for complete guides " +
        "(e.g. \"The Complete Guide to Python\"). Target 3,000+ words of topical authority, structured problem-solving, and " +
        "long-tail keyword coverage without padding.";

    // BlogPosting companion articles — listicles, how-tos, evergreen depth.
    public const int BlogMinWords = 1_500;
    public const int BlogTargetMinWords = 1_500;
    public const int BlogTargetMaxWords = 2_500;
    public const int BlogSectionMinWords = 280;
    public const int BlogSectionTargetMaxWords = 450;
    public const int BlogSectionCountMin = 5;
    public const int BlogSectionCountTarget = 6;

    public const string BlogEditorialDefinition =
        "Blog postings in the 1,500–2,500 word range support keyword synonyms, internal linking, and strong on-page structure. " +
        "Use headers, bullets, and short paragraphs so listicles and how-to guides keep readers engaged.";

    // Standard Listicles & Guides — actionable step-by-step tutorials (future content type).
    public const int ListicleGuideMinWords = 1_200;
    public const int ListicleGuideMaxWords = 1_800;

    public const string ListicleGuideEditorialDefinition =
        "Standard listicles and guides are actionable, step-by-step tutorials that require substantial context, " +
        "data, and layout formatting. Used when the reader needs a practical walkthrough, not a macro overview.";

    // Tool pages — TechnicalArticle comprehensive guides for each platform in the pillar Top AI Tools section.
    public const int ToolMinWords = 1_500;
    public const int ToolTargetMinWords = 1_500;
    public const int ToolTargetMaxWords = 2_500;
    public const int ToolHardMaxWords = 2_500;

    public const string ToolEditorialDefinition =
        "Tool pages are TechnicalArticle comprehensive guides for a single platform — deep implementation context, " +
        "capabilities, architectural fit, and when to use it. Target 1,500–2,500 words (same band as comprehensive " +
        "guides and tutorials); one platform per page.";

    public static string PillarRangeLabel => $"{PillarMinWords:N0}+";
    public static string BlogRangeLabel => $"{BlogTargetMinWords:N0}–{BlogTargetMaxWords:N0}";
    public static string ListicleGuideRangeLabel => $"{ListicleGuideMinWords:N0}–{ListicleGuideMaxWords:N0}";
    public static string ToolRangeLabel => $"{ToolMinWords:N0}–{ToolTargetMaxWords:N0}";

    // Email — Cold Outreach / Sales (implemented).
    public const int EmailColdOutreachMinWords = 50;
    public const int EmailColdOutreachMaxWords = 125;

    public const string EmailColdOutreachEditorialDefinition =
        "Cold outreach and sales emails aim for high response rates with a single, clear call-to-action.";

    // Email stubs (constants only — no generation yet).
    public const int EmailNewsletterMinWords = 200;
    public const int EmailNewsletterMaxWords = 400;

    public const string EmailNewsletterEditorialDefinition =
        "Curated newsletters summarize external links and drive traffic back to the website.";

    public const int EmailStoryNurtureMinWords = 500;
    public const int EmailStoryNurtureMaxWords = 1_000;

    public const string EmailStoryNurtureEditorialDefinition =
        "Story-based nurture emails build deep trust and treat email like an exclusive blog post.";

    public const int EmailTransactionalMinWords = 1;
    public const int EmailTransactionalMaxWords = 49;

    public const string EmailTransactionalEditorialDefinition =
        "Transactional emails deliver critical data; highly functional with zero fluff.";

    public static string EmailColdOutreachRangeLabel =>
        $"{EmailColdOutreachMinWords}–{EmailColdOutreachMaxWords}";
}
