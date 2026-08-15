using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class PageContextBuilderTests
{
    [Fact]
    public void Serializes_nested_headings_to_markdown_and_flat_heading_list()
    {
        const string html = """
            <html><head><title>Workflow</title><meta name="description" content="Plan campaigns."></head><body>
              <h1>AI Content Creation Workflow</h1>
              <p>An overview paragraph about the workflow.</p>
              <h2>Marketing</h2>
              <p>Marketing teams use this workflow to plan campaigns.</p>
              <h3>Social Media</h3>
              <p>Social posts are generated from the same brief.</p>
            </body></html>
            """;

        var ctx = PageContextBuilder.FromHtml(html);

        Assert.Equal("Workflow", ctx.Title);
        Assert.Equal("Plan campaigns.", ctx.Description);
        Assert.Equal(
            ["AI Content Creation Workflow", "Marketing", "Social Media"],
            ctx.Headings);
        Assert.Contains("# AI Content Creation Workflow", ctx.MainContentMarkdown);
        Assert.Contains("## Marketing", ctx.MainContentMarkdown);
        Assert.Contains("### Social Media", ctx.MainContentMarkdown);
        Assert.Contains("An overview paragraph about the workflow.", ctx.MainContentMarkdown);
    }

    [Fact]
    public void Tool_list_anchors_become_markdown_links_under_heading()
    {
        const string html = """
            <h6>Top 5 Automated Data Entry Processing Tools:</h6>
            <ul>
              <li><a href="/tools/zapier">Zapier</a></li>
              <li><a href="/tools/quickbooks">QuickBooks</a></li>
            </ul>
            """;

        var ctx = PageContextBuilder.FromHtml(html);

        Assert.Contains("Top 5 Automated Data Entry Processing Tools:", ctx.Headings);
        Assert.Contains("###### Top 5 Automated Data Entry Processing Tools:", ctx.MainContentMarkdown);
        Assert.Contains("- [Zapier](/tools/zapier)", ctx.MainContentMarkdown);
        Assert.Contains("- [QuickBooks](/tools/quickbooks)", ctx.MainContentMarkdown);
        Assert.DoesNotContain("<a ", ctx.MainContentMarkdown);
        Assert.DoesNotContain("<h6", ctx.MainContentMarkdown);
    }
}
