using ContentWriter.Domain.Enums;

namespace ContentWriter.Domain.Entities;

public class GeneratedContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public GeneratedContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Clean H1 for the live page. Falls back to <see cref="Title"/> when unset.</summary>
    public string? DisplayTitle { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>Listing deck / newspaper wire summary. Published to GeekAPI excerpt columns.</summary>
    public string ListingExcerpt { get; set; } = string.Empty;

    public string? MetaDescription { get; set; }

    /// <summary>Tool-page callout copy (tool posts only).</summary>
    public string? AdvertisingExcerpt { get; set; }

    [Obsolete("Hero art uses convention path hero.avif on disk, not a stored URL.")]
    public string? HeroImageUrl { get; set; }

    /// <summary>Top Tools app name this tool row was generated from (tool posts only).</summary>
    public string? SourceAppName { get; set; }

    /// <summary>Order within the pillar Top Tools section (tool posts only).</summary>
    public int? SourceAppOrder { get; set; }
    public List<string> Keywords { get; set; } = new();
    public int WordCount { get; set; }

    /// <summary>H2 section topics from the plan step; guides the body step.</summary>
    public List<string> SectionOutline { get; set; } = new();

    /// <summary>Serialized JSON+LD object (TechnicalArticle or BlogPosting schema). Null for social posts.</summary>
    public string? JsonLdSchema { get; set; }

    /// <summary>For blog posts: the canonical URL/anchor of the TechnicalArticle it links back to.</summary>
    public string? RelatedArticleUrl { get; set; }

    public LlmProviderType GeneratedByProvider { get; set; }
    public string GeneratedByModel { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
