namespace ContentWriter.Application.Services;

/// <summary>Recommended word-count ranges by content type.</summary>
public static class ContentLengthTargets
{
    // Pillar Pages & Guides — exhaustive hubs linking to cluster articles.
    public const int PillarMinWords = 3_000;
    public const int PillarTargetMinWords = 3_500;
    public const int PillarTargetMaxWords = 5_000;
    public const int PillarSectionMinWords = 500;
    public const int PillarSectionTargetMaxWords = 700;
    public const int PillarToolsSectionMinWords = 700;
    public const int PillarToolsSectionTargetMaxWords = 900;

    // How-To Guides & Listicles — companion blog posts for lead conversion.
    public const int BlogMinWords = 1_700;
    public const int BlogTargetMinWords = 1_700;
    public const int BlogTargetMaxWords = 2_500;
    public const int BlogSectionMinWords = 350;
    public const int BlogSectionTargetMaxWords = 500;

    // Standard informational posts (reference for future content types).
    public const int StandardPostMinWords = 1_400;
    public const int StandardPostMaxWords = 1_500;

    // News & quick updates (reference for future content types).
    public const int NewsMinWords = 400;
    public const int NewsMaxWords = 800;

    public static string PillarRangeLabel => $"{PillarMinWords:N0}–{PillarTargetMaxWords:N0}+";
    public static string BlogRangeLabel => $"{BlogTargetMinWords:N0}–{BlogTargetMaxWords:N0}";
}
