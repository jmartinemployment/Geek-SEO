namespace GeekSeo.Application.Models.Seo;

public sealed class ContentBlogSpoke
{
    public string Slug { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string SpokeType { get; set; } = "comparison";
    public string Title { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class ContentBlogSpokeValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }
}

public sealed record GenerateBlogSpokeRequest
{
    public string SpokeType { get; init; } = "comparison";
    public string? SpokeKeyword { get; init; }
}
