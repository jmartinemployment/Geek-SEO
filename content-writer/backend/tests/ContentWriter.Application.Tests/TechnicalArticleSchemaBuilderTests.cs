using System.Text.Json;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Services.SchemaBuilders;

namespace ContentWriter.Application.Tests;

public class TechnicalArticleSchemaBuilderTests
{
    [Fact]
    public void BuildToolOverview_emits_TechnicalArticle_with_pillar_citation_and_software_graph()
    {
        var builder = new TechnicalArticleSchemaBuilder(new SoftwareApplicationSchemaBuilder());
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

        var json = builder.BuildToolOverview(
            metadata,
            "https://www.geekatyourspot.com/use-cases/accounting/ai-for-accounting",
            new SoftwareApplicationDescriptor("HubSpot", "CRM platform"));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("@graph", out var graph));
        var article = graph.EnumerateArray().First(e =>
            e.GetProperty("@type").GetString() == "TechnicalArticle");
        Assert.Equal("https://www.geekatyourspot.com/tools/accounting/hubspot",
            article.GetProperty("mainEntityOfPage").GetProperty("@id").GetString());
        Assert.Equal("TechnicalArticle",
            article.GetProperty("citation")[0].GetProperty("@type").GetString());
        Assert.Equal("https://www.geekatyourspot.com/use-cases/accounting/ai-for-accounting",
            article.GetProperty("citation")[0].GetProperty("url").GetString());
        Assert.Equal(650, article.GetProperty("wordCount").GetInt32());
        Assert.True(graph.EnumerateArray().Any(e =>
            e.GetProperty("@type").GetString() == "SoftwareApplication"));
        Assert.DoesNotContain("NewsArticle", json, StringComparison.Ordinal);
        Assert.DoesNotContain("BlogPosting", json, StringComparison.Ordinal);
    }
}
