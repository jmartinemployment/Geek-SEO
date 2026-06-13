using GeekSeo.Application.Interfaces.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed partial class HtmlRichTextProvider : IRichTextProvider
{
    public string ProviderName => "html";

    public string ExtractPlainText(string html) =>
        HtmlTextUtility.StripHtml(html);

    public int CountWords(string html)
    {
        var text = ExtractPlainText(html);
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
