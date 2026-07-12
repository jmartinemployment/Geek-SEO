using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Tests;

public class MergedFigureMarkupTests
{
    [Fact]
    public void Strip_removes_legacy_inline_figure_blocks()
    {
        const string body = """
            ## Introduction

            <figure data-figure-key="pillar:intro" data-geek-figure="1"><img src="https://old.example/a.avif" alt="old" /></figure>

            Paragraph text.
            """;

        var stripped = MergedFigureMarkup.Strip(body);

        Assert.DoesNotContain("data-geek-figure", stripped);
        Assert.Contains("Paragraph text.", stripped);
    }
}
