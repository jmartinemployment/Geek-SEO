namespace ContentWriter.Application.Services.Publish;

internal static class GeneratedBodyHtmlNormalizer
{
    public static string Normalize(string html) =>
        JunkBodySectionFilter.StripJunkSectionsFromHtml(BodyPreambleFilter.StripPreambleFromHtml(html));
}
