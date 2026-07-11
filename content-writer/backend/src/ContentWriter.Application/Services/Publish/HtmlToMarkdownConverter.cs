using ReverseMarkdown;

namespace ContentWriter.Application.Services.Publish;

public static class HtmlToMarkdownConverter
{
    private static readonly Converter Converter = CreateConverter();

    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var trimmed = html.Trim();
        if (!LooksLikeHtml(trimmed))
            return trimmed;

        return Converter.Convert(trimmed).Trim();
    }

    private static bool LooksLikeHtml(string value) =>
        value.StartsWith('<') || value.Contains("</", StringComparison.Ordinal);

    private static Converter CreateConverter()
    {
        var config = new Config
        {
            UnknownTags = Config.UnknownTagsOption.Drop,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true,
        };

        return new Converter(config);
    }
}
