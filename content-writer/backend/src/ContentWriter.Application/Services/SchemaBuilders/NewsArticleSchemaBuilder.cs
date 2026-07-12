using System.Text.Json;
using System.Text.Json.Serialization;
using ContentWriter.Application.DTOs;

namespace ContentWriter.Application.Services.SchemaBuilders;

public interface INewsArticleSchemaBuilder
{
    /// <summary>Builds a schema.org NewsArticle JSON+LD document that cites the parent TechnicalArticle.</summary>
    string Build(
        ContentMetadata metadata,
        string pillarArticleUrl,
        SoftwareApplicationDescriptor? about = null);
}

public class NewsArticleSchemaBuilder : INewsArticleSchemaBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string Build(
        ContentMetadata metadata,
        string pillarArticleUrl,
        SoftwareApplicationDescriptor? about = null)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "NewsArticle",
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
            ["citation"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "TechnicalArticle",
                    ["url"] = pillarArticleUrl
                }
            }
        };

        if (about is not null && !string.IsNullOrWhiteSpace(about.Name))
        {
            schema["about"] = new Dictionary<string, object?>
            {
                ["@type"] = "SoftwareApplication",
                ["name"] = about.Name.Trim(),
                ["description"] = string.IsNullOrWhiteSpace(about.Description) ? null : about.Description.Trim()
            };
        }

        return JsonSerializer.Serialize(schema, JsonOptions);
    }
}
