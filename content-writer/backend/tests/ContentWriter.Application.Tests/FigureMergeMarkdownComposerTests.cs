using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Tests;

public class FigureMergeMarkdownComposerTests
{
    private static readonly FigureMergeBlock IntroFigure = new(
        FigureSourceType.Pillar,
        "intro",
        "Introduction",
        1,
        "https://blob.example/intro.webp",
        "Diagram: Introduction",
        1200,
        800,
        FigureStatus.Ready);

    private static readonly FigureMergeBlock NextFigure = new(
        FigureSourceType.Pillar,
        "next-steps",
        "Next Steps",
        2,
        "https://blob.example/next.webp",
        "Diagram: Next Steps",
        1200,
        800,
        FigureStatus.Ready);

    [Fact]
    public void StripMergedFigures_removes_existing_blocks()
    {
        const string body = """
            ## Introduction

            <figure data-figure-key="pillar:intro" data-geek-figure="1"><img src="https://old.example/a.webp" alt="old" /></figure>

            Paragraph text.
            """;

        var stripped = FigureMergeMarkdownComposer.StripMergedFigures(body);

        Assert.DoesNotContain("data-geek-figure", stripped);
        Assert.Contains("Paragraph text.", stripped);
    }

    [Fact]
    public void MergeFiguresIntoBody_inserts_after_matching_h2()
    {
        const string body = """
            ## Introduction

            Opening paragraph.

            ## Next Steps

            Closing paragraph.
            """;

        var merged = FigureMergeMarkdownComposer.MergeFiguresIntoBody(body, [IntroFigure, NextFigure]);

        Assert.Contains("data-figure-key=\"pillar:intro\"", merged);
        Assert.Contains("https://blob.example/intro.webp", merged);
        Assert.Contains("fetchpriority=\"high\"", merged);
        Assert.Contains("data-figure-key=\"pillar:next-steps\"", merged);
        Assert.Contains("loading=\"lazy\"", merged);
    }

    [Fact]
    public void MergeFiguresIntoBody_is_idempotent_when_re_run()
    {
        const string body = """
            ## Introduction

            Body copy.
            """;

        var first = FigureMergeMarkdownComposer.MergeFiguresIntoBody(body, [IntroFigure]);
        var second = FigureMergeMarkdownComposer.MergeFiguresIntoBody(first, [IntroFigure]);

        Assert.Equal(
            FigureMergeMarkdownComposer.StripMergedFigures(first).Trim(),
            FigureMergeMarkdownComposer.StripMergedFigures(second).Trim());
        Assert.Equal(1, second.Split("data-geek-figure=\"1\"", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void ResolveHeroFigureKey_uses_lowest_section_order()
    {
        var heroKey = FigureMergeMarkdownComposer.ResolveHeroFigureKey([NextFigure, IntroFigure]);

        Assert.Equal("pillar:intro", heroKey);
    }
}

public class FigureSchemaMetadataHelperTests
{
    [Fact]
    public void WithHeroImage_sets_image_property()
    {
        var json = FigureSchemaMetadataHelper.WithHeroImage(
            """{"@type":"TechnicalArticle","headline":"Test"}""",
            "https://blob.example/hero.webp");

        Assert.Contains("\"image\":\"https://blob.example/hero.webp\"", json.Replace(" ", ""));
    }
}
