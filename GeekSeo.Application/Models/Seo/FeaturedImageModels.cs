namespace GeekSeo.Application.Models.Seo;

public sealed record FeaturedImageResult
{
    public required string DataUrl { get; init; }
    public required string Prompt { get; init; }
    public required string MimeType { get; init; }
}

public sealed record GenerateFeaturedImageRequest
{
  public bool Regenerate { get; init; }
}
