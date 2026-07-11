using System.Text;
using System.Text.RegularExpressions;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services.Figures;

public sealed record FigureMergeBlock(
    string SourceType,
    string HeadingSlug,
    string Heading,
    int SectionOrder,
    string ImageUrl,
    string ImageAlt,
    int? ImageWidth,
    int? ImageHeight,
    FigureStatus Status);

public static partial class FigureMergeMarkdownComposer
{
    [GeneratedRegex(
        @"<figure\s[^>]*data-geek-figure=""1""[^>]*>.*?</figure>\s*",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MergedFigureRegex();

    public static string StripMergedFigures(string body) =>
        MergedFigureRegex().Replace(body, string.Empty);

    public static string MergeFiguresIntoBody(
        string body,
        IReadOnlyList<FigureMergeBlock> figures)
    {
        if (figures.Count == 0)
        {
            return StripMergedFigures(body);
        }

        var stripped = StripMergedFigures(body);
        var byHeading = figures
            .GroupBy(f => f.Heading.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var heroKey = ResolveHeroFigureKey(figures);
        var lines = stripped.ReplaceLineEndings("\n").Split('\n');
        var output = new List<string>(lines.Length + figures.Count);

        foreach (var line in lines)
        {
            output.Add(line);
            if (!line.StartsWith("## ", StringComparison.Ordinal))
            {
                continue;
            }

            var heading = line[3..].Trim();
            if (!byHeading.TryGetValue(heading, out var figure))
            {
                continue;
            }

            output.Add(BuildFigureHtml(figure, heroKey));
        }

        return string.Join('\n', output);
    }

    public static string? ResolveHeroFigureKey(IReadOnlyList<FigureMergeBlock> figures)
    {
        var hero = figures
            .Where(f => f.Status is FigureStatus.Ready or FigureStatus.Published)
            .OrderBy(f => f.SectionOrder)
            .FirstOrDefault();

        return hero is null ? null : FigureKey(hero.SourceType, hero.HeadingSlug);
    }

    public static string FigureKey(string sourceType, string headingSlug) =>
        $"{sourceType}:{headingSlug}";

    private static string BuildFigureHtml(FigureMergeBlock figure, string? heroFigureKey)
    {
        var figureKey = FigureKey(figure.SourceType, figure.HeadingSlug);
        var isHero = heroFigureKey is not null
                     && figureKey.Equals(heroFigureKey, StringComparison.OrdinalIgnoreCase);

        var attrs = new StringBuilder();
        attrs.Append($"src=\"{HtmlEncodeAttr(figure.ImageUrl)}\"");
        attrs.Append($" alt=\"{HtmlEncodeAttr(figure.ImageAlt)}\"");
        if (figure.ImageWidth is > 0)
        {
            attrs.Append($" width=\"{figure.ImageWidth.Value}\"");
        }

        if (figure.ImageHeight is > 0)
        {
            attrs.Append($" height=\"{figure.ImageHeight.Value}\"");
        }

        if (isHero)
        {
            attrs.Append(" fetchpriority=\"high\"");
        }
        else
        {
            attrs.Append(" loading=\"lazy\"");
        }

        return $"<figure data-figure-key=\"{HtmlEncodeAttr(figureKey)}\" data-geek-figure=\"1\"><img {attrs} /></figure>";
    }

    private static string HtmlEncodeAttr(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
