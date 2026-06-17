namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// What the site *is* (schema + title), used to filter SERP competitors that only rank on
/// audience-industry pillars (page_vertical) such as "Accounting" on an AI consulting homepage.
/// </summary>
internal sealed record SiteBusinessProfile(IReadOnlySet<string> CoreTopicTokens);
