namespace ContentWriter.Application.Services;

/// <summary>Recommended word-count ranges and editorial definitions by content type.</summary>
public static class ContentLengthTargets
{
    // Pillar Pages — exhaustive macro-level hubs linking to cluster articles.
    public const int PillarMinWords = 3_000;
    public const int PillarTargetMinWords = 3_500;
    public const int PillarTargetMaxWords = 5_000;
    public const int PillarSectionMinWords = 500;
    public const int PillarSectionTargetMaxWords = 700;
    public const int PillarToolsSectionMinWords = 700;
    public const int PillarToolsSectionTargetMaxWords = 900;

    public const string PillarEditorialDefinition =
        "Pillar pages are exhaustive, macro-level entry points for massive topics. They host multiple subsections " +
        "and link out to smaller cluster articles. Quality means comprehensive coverage — not padding.";

    // Deep-Dive Blog Posts — companion articles aimed at outranking competitors.
    public const int BlogMinWords = 1_800;
    public const int BlogTargetMinWords = 1_800;
    public const int BlogTargetMaxWords = 2_500;
    public const int BlogSectionMinWords = 400;
    public const int BlogSectionTargetMaxWords = 550;
    public const int BlogSectionCountMin = 5;
    public const int BlogSectionCountTarget = 6;

    public const string BlogEditorialDefinition =
        "Deep-dive blog posts are the sweet spot for standard articles trying to outrank competitors on search engines. " +
        "Each section must add real depth: context, examples, data, and actionable insight — not surface summaries.";

    // Standard Listicles & Guides — actionable step-by-step tutorials (future content type).
    public const int ListicleGuideMinWords = 1_200;
    public const int ListicleGuideMaxWords = 1_800;

    public const string ListicleGuideEditorialDefinition =
        "Standard listicles and guides are actionable, step-by-step tutorials that require substantial context, " +
        "data, and layout formatting. Used when the reader needs a practical walkthrough, not a macro overview.";

    // News & quick updates (reference for future content types).
    public const int NewsMinWords = 400;
    public const int NewsMaxWords = 800;

    public const string NewsEditorialDefinition =
        "News and quick updates are best for press releases or short announcements — timely, concise, and direct.";

    public static string PillarRangeLabel => $"{PillarMinWords:N0}–{PillarTargetMaxWords:N0}+";
    public static string BlogRangeLabel => $"{BlogTargetMinWords:N0}–{BlogTargetMaxWords:N0}";
    public static string ListicleGuideRangeLabel => $"{ListicleGuideMinWords:N0}–{ListicleGuideMaxWords:N0}";
}

