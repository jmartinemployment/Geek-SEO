namespace GeekSeo.Application.Services.Seo;

public sealed record EeatApplyContext
{
    public required string Keyword { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string OrganizationUrl { get; init; } = string.Empty;
    public string BusinessSummary { get; init; } = string.Empty;
    public string? FeaturedImageUrl { get; init; }
}
