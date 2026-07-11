namespace GeekSeo.Application.Models.Seo;

public sealed record ContentSocialPostResult
{
    public required string FacebookPost { get; init; }
    public required string LinkedInPost { get; init; }
    public required string Keyword { get; init; }
    public string? BlogPostSlug { get; init; }
}

public sealed record GenerateSocialPostRequest
{
    public string? BlogPostTitle { get; init; }
    public string? BlogPostSlug { get; init; }
}
