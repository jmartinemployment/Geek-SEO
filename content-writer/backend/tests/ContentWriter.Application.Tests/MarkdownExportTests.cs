using ContentWriter.Application.DTOs;
using ContentWriter.Application.Services.Export;

namespace ContentWriter.Application.Tests;

public class DepartmentNameResolverTests
{
    [Theory]
    [InlineData("https://www.geekatyourspot.com/use-cases/accounting/smart-bank-reconciliation", "accounting")]
    [InlineData("https://www.geekatyourspot.com/use-cases/marketing", "marketing")]
    public void Resolve_extracts_department_from_use_cases_url(string url, string expected)
    {
        var result = DepartmentNameResolver.Resolve(url, null, null, null, null);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_prefers_explicit_override()
    {
        var result = DepartmentNameResolver.Resolve(
            "https://www.geekatyourspot.com/use-cases/accounting/foo",
            null,
            null,
            "Sales Project",
            "human-resources");

        Assert.Equal("human-resources", result);
    }

    [Fact]
    public void Resolve_uses_project_name_prefix_when_no_url_match()
    {
        var result = DepartmentNameResolver.Resolve(null, null, null, "Accounting - Smart Bank Recon", null);
        Assert.Equal("accounting", result);
    }
}

public class MarkdownExportDocumentBuilderTests
{
    [Fact]
    public void Build_includes_frontmatter_body_and_json_ld()
    {
        var markdown = MarkdownExportDocumentBuilder.Build(new MarkdownExportInput(
            Title: "Smart Bank Reconciliation",
            Slug: "smart-bank-reconciliation",
            MetaDescription: "Match feeds to the ledger.",
            CanonicalUrl: "https://www.geekatyourspot.com/use-cases/accounting/smart-bank-reconciliation",
            ContentType: "pillar",
            Department: "accounting",
            WordCount: 3200,
            Keywords: ["bank reconciliation", "ledger coding"],
            RelatedUrl: "https://www.geekatyourspot.com/blog/smart-bank-reconciliation",
            BodyHtml: "<article><h2>Overview</h2><p>Body copy.</p></article>",
            JsonLdSchema: "{\n  \"@type\": \"TechArticle\"\n}",
            ExportedAtUtc: new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Contains("title: Smart Bank Reconciliation", markdown);
        Assert.Contains("contentType: pillar", markdown);
        Assert.Contains("department: accounting", markdown);
        Assert.Contains("<article><h2>Overview</h2><p>Body copy.</p></article>", markdown);
        Assert.Contains("## JSON-LD Schema", markdown);
        Assert.Contains("\"@type\": \"TechArticle\"", markdown);
    }

    [Fact]
    public void BuildSocial_includes_platform_and_body()
    {
        var markdown = MarkdownExportDocumentBuilder.BuildSocial(
            new SocialPostDraft("Facebook", "Short social copy."),
            "accounting",
            "smart-bank-reconciliation",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));

        Assert.Contains("platform: Facebook", markdown);
        Assert.Contains("contentType: social", markdown);
        Assert.Contains("Short social copy.", markdown);
    }

    [Fact]
    public void BuildColdOutreach_includes_subject_and_cta()
    {
        var markdown = MarkdownExportDocumentBuilder.BuildColdOutreach(
            new ColdOutreachEmailContent(
                "Reconcile faster",
                "Body text here.",
                "Read the guide",
                "https://example.com/pillar"),
            "accounting",
            "smart-bank-reconciliation",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));

        Assert.Contains("subject: Reconcile faster", markdown);
        Assert.Contains("https://example.com/pillar", markdown);
        Assert.Contains("Body text here.", markdown);
    }

    [Fact]
    public void BuildImagePrompt_includes_leonardo_settings()
    {
        var markdown = MarkdownExportDocumentBuilder.BuildImagePrompt(
            new ImagePromptContent(
                "Pillar figure",
                "Flat vector diagram of bank feeds.",
                1536,
                1024,
                "Leonardo Phoenix",
                "phoenix-id",
                "Illustration",
                true,
                false,
                null),
            "accounting",
            "smart-bank-reconciliation",
            new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));

        Assert.Contains("useCase: Pillar figure", markdown);
        Assert.Contains("leonardoModel: Leonardo Phoenix", markdown);
        Assert.Contains("Flat vector diagram of bank feeds.", markdown);
    }
}
