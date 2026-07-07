using ContentWriter.Domain.Enums;

namespace ContentWriter.Domain.Entities;

public class GeneratedContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public GeneratedContentType ContentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;

    public string? MetaDescription { get; set; }
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
