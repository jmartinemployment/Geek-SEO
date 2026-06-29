namespace SiteAnalyzer2.Services.BusinessFocus;

public record BusinessFocusInput(
    string TargetSiteUrl,
    IReadOnlyList<string> Headings,
    IReadOnlyList<string> MetaTags,
    IReadOnlyList<string> JsonLdBlocks,
    IReadOnlyList<string> ContentBlocks,
    bool HasExistingSchema);

public record BusinessFocusClassificationResult(
    string BusinessType,
    IReadOnlyList<string> PrimaryServices,
    string? ServiceArea,
    string Description,
    string GeneratedSchemaJson,
    bool HasExistingSchema,
    bool? ExistingSchemaMatches);
