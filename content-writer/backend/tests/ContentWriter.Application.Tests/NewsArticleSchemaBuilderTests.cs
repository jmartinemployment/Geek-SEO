using System.Text.Json;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Services.SchemaBuilders;

namespace ContentWriter.Application.Tests;

public class NewsArticleSchemaBuilderTests
{
    [Fact]
    public void Build_emits_NewsArticle_with_pillar_citation_and_optional_about()
    {
        var builder = new NewsArticleSchemaBuilder();
        var metadata = new ContentMetadata(
            "HubSpot",
            "SEO summary for HubSpot in accounting workflows.",
            "Author",
            "Publisher",
            "https://example.com/logo.png",
            "https://www.geekatyourspot.com/tools/accounting/hubspot",
            "https://example.com/logo.png",
            new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 12, 12, 0, 0, DateTimeKind.Utc),
            ["ai accounting"],
            650);

        var json = builder.Build(
            metadata,
            "https://www.geekatyourspot.com/use-cases/accounting/ai-for-accounting",
            new SoftwareApplicationDescriptor("HubSpot", "CRM platform"));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("NewsArticle", root.GetProperty("@type").GetString());
        Assert.Equal("https://www.geekatyourspot.com/tools/accounting/hubspot",
            root.GetProperty("mainEntityOfPage").GetProperty("@id").GetString());
        Assert.Equal("TechnicalArticle",
            root.GetProperty("citation")[0].GetProperty("@type").GetString());
        Assert.Equal("https://www.geekatyourspot.com/use-cases/accounting/ai-for-accounting",
            root.GetProperty("citation")[0].GetProperty("url").GetString());
        Assert.Equal("SoftwareApplication",
            root.GetProperty("about").GetProperty("@type").GetString());
        Assert.Equal(650, root.GetProperty("wordCount").GetInt32());
        Assert.DoesNotContain("BlogPosting", json, StringComparison.Ordinal);
    }
}
