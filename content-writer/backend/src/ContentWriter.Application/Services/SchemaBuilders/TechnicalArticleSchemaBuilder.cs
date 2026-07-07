using System.Text.Json;
using System.Text.Json.Serialization;
using ContentWriter.Application.DTOs;

namespace ContentWriter.Application.Services.SchemaBuilders;

public interface ITechnicalArticleSchemaBuilder
{
    /// <summary>Builds a schema.org TechnicalArticle JSON+LD document that cites the companion blog post.</summary>
    string Build(ContentMetadata metadata, string relatedBlogPostUrl);
}

public class TechnicalArticleSchemaBuilder : ITechnicalArticleSchemaBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string Build(ContentMetadata metadata, string relatedBlogPostUrl)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "TechnicalArticle",
            ["headline"] = metadata.Headline,
            ["description"] = metadata.Description,
            ["image"] = new[] { metadata.MainImageUrl },
            ["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["name"] = metadata.AuthorName
            },
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = metadata.PublisherName,
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = metadata.PublisherLogoUrl
                }
            },
            ["datePublished"] = metadata.DatePublishedUtc.ToString("O"),
            ["dateModified"] = metadata.DateModifiedUtc.ToString("O"),
            ["mainEntityOfPage"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebPage",
                ["@id"] = metadata.CanonicalUrl
            },
            ["keywords"] = string.Join(", ", metadata.Keywords),
            ["wordCount"] = metadata.WordCount,
            ["proficiencyLevel"] = "Beginner",
            // Cross-link to the companion blog post output, per spec: the article output must
            // contain an anchor/link to the blog post output.
            ["citation"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "BlogPosting",
                    ["url"] = relatedBlogPostUrl
                }
            }
        };

        return JsonSerializer.Serialize(schema, JsonOptions);
    }
}
