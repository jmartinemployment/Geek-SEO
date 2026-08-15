using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class ContentExtractorTests
{
    [Fact]
    public void ExtractTools_from_markdown_with_heading_department()
    {
        var markdown = @"
# Tools & Resources

- [Figma](https://figma.com)
- [VS Code](https://code.visualstudio.com)

# External Links

- [Google](https://google.com)
- [GitHub](https://github.com)
";
        var context = new PageContext
        {
            Title = "Test Page",
            Description = "Test",
            MainContentMarkdown = markdown
        };

        var tools = ContentExtractor.ExtractTools(context);

        Assert.Equal(4, tools.Count);

        // First heading department
        var figma = tools.FirstOrDefault(t => t.Name == "Figma");
        Assert.NotNull(figma);
        Assert.Equal("Figma", figma.Name);
        Assert.Equal("https://figma.com", figma.Href);
        Assert.Equal("Tools & Resources", figma.Department);

        var vscode = tools.FirstOrDefault(t => t.Name == "VS Code");
        Assert.NotNull(vscode);
        Assert.Equal("Tools & Resources", vscode.Department);

        // Second heading department
        var google = tools.FirstOrDefault(t => t.Name == "Google");
        Assert.NotNull(google);
        Assert.Equal("External Links", google.Department);

        var github = tools.FirstOrDefault(t => t.Name == "GitHub");
        Assert.NotNull(github);
        Assert.Equal("External Links", github.Department);
    }

    [Fact]
    public void ExtractTools_allows_duplicates_in_different_departments()
    {
        var markdown = @"
# Department A

- [Tool](https://example.com)

# Department B

- [Tool](https://example.com)
";
        var context = new PageContext
        {
            Title = "Test",
            Description = "Test",
            MainContentMarkdown = markdown
        };

        var tools = ContentExtractor.ExtractTools(context);

        Assert.Equal(2, tools.Count);
        var deptA = tools.First(t => t.Department == "Department A");
        var deptB = tools.First(t => t.Department == "Department B");
        Assert.Equal("Tool", deptA.Name);
        Assert.Equal("Tool", deptB.Name);
        // Same href, same name, but different department
    }

    [Fact]
    public void ExtractTools_returns_empty_for_null_context()
    {
        var tools = ContentExtractor.ExtractTools(null!);
        Assert.Empty(tools);
    }

    [Fact]
    public void ExtractTools_returns_empty_for_empty_markdown()
    {
        var context = new PageContext
        {
            Title = "Test",
            Description = "Test",
            MainContentMarkdown = ""
        };

        var tools = ContentExtractor.ExtractTools(context);
        Assert.Empty(tools);
    }

    [Fact]
    public void ExtractTools_ignores_malformed_links()
    {
        var markdown = @"
# Tools

- [Valid](https://example.com)
- [Missing URL]
- Missing brackets https://example.com
- [Incomplete](
";
        var context = new PageContext
        {
            Title = "Test",
            Description = "Test",
            MainContentMarkdown = markdown
        };

        var tools = ContentExtractor.ExtractTools(context);

        // Only the valid link should be extracted
        Assert.Single(tools);
        Assert.Equal("Valid", tools[0].Name);
    }

    [Fact]
    public void ExtractTools_uses_first_heading_as_department_for_links_without_preceding_heading()
    {
        var markdown = @"
# Main Department

- [Tool1](https://example.com)
- [Tool2](https://example.com)
";
        var context = new PageContext
        {
            Title = "Test",
            Description = "Test",
            MainContentMarkdown = markdown
        };

        var tools = ContentExtractor.ExtractTools(context);

        Assert.Equal(2, tools.Count);
        Assert.All(tools, t => Assert.Equal("Main Department", t.Department));
    }

    [Fact]
    public void ComputeContextHash_produces_consistent_hash()
    {
        var json = """{"title":"Test","description":"Test"}""";

        var hash1 = ContentExtractor.ComputeContextHash(json);
        var hash2 = ContentExtractor.ComputeContextHash(json);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeContextHash_differs_for_different_json()
    {
        var json1 = """{"title":"Test1"}""";
        var json2 = """{"title":"Test2"}""";

        var hash1 = ContentExtractor.ComputeContextHash(json1);
        var hash2 = ContentExtractor.ComputeContextHash(json2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeContextHash_handles_empty_string()
    {
        var hash = ContentExtractor.ComputeContextHash("");
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA256 hex is 64 chars
    }
}
