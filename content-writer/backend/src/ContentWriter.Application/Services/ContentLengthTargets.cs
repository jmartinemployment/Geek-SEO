namespace ContentWriter.Application.Services;

/// <summary>Recommended word-count ranges and editorial definitions by content type.</summary>
public static class ContentLengthTargets
{
    // TechnicalArticle pillar pages — topical authority, tutorials, long-tail keywords.
    public const int PillarMinWords = 1_500;
    public const int PillarTargetMinWords = 1_500;
    public const int PillarTargetMaxWords = 2_500;
    public const int PillarSectionMinWords = 250;
    public const int PillarSectionTargetMaxWords = 450;
    public const int PillarToolsSectionMinWords = 350;
    public const int PillarToolsSectionTargetMaxWords = 550;

    public const string PillarEditorialDefinition =
        "Technical articles build topical authority with in-depth explanations, structured problem-solving, and room for " +
        "long-tail keywords. Target depth that exceeds competing pages — comprehensive tutorials and trustworthy detail, not padding.";

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

    // Tool pages — TechnicalArticle overviews for each platform in the pillar Top AI Tools section.
    public const int ToolMinWords = 600;
    public const int ToolTargetMaxWords = 1_000;
    public const int ToolHardMaxWords = 1_000;

    public const string ToolEditorialDefinition =
        "Tool pages are TechnicalArticle overviews of a single platform — implementation context, capabilities, " +
        "and when to use it. Stay focused (600–1,000 words); one platform per page.";

    public static string PillarRangeLabel => $"{PillarMinWords:N0}–{PillarTargetMaxWords:N0}+";
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
