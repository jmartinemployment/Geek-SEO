using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class EeatAdvisoryBuilder
{
    private static readonly string[] ExperiencePhrases =
        ["we installed", "our team", "i tested", "in our experience", "we found that"];

    public static IReadOnlyList<EeatAdvisory> Build(string plainText, string contentHtml)
    {
        var advisories = new List<EeatAdvisory>();
        var lower = plainText.ToLowerInvariant();

        if (!ExperiencePhrases.Any(p => lower.Contains(p, StringComparison.Ordinal)))
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "first_hand_experience",
                ActionText = "Add a short section describing first-hand experience with this topic.",
            });
        }

        if (!contentHtml.Contains("schema.org", StringComparison.OrdinalIgnoreCase)
            && !contentHtml.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase))
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "author_schema",
                ActionText = "Add Article schema with an author property to strengthen trust signals.",
            });
        }

        if (!lower.Contains("about the author", StringComparison.Ordinal) && !lower.Contains("written by", StringComparison.Ordinal))
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "author_bio",
                ActionText = "Include a visible author bio with relevant credentials or local expertise.",
            });
        }

        if (!HasOutboundCitation(contentHtml))
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "source_citations",
                ActionText = "Cite reputable external sources to support factual claims.",
            });
        }

        if (CountOccurrences(contentHtml, "<img") == 0)
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "original_media",
                ActionText = "Add original photos or diagrams — stock-only pages weaken E-E-A-T signals.",
            });
        }

        if (!lower.Contains("updated", StringComparison.Ordinal) && !lower.Contains("reviewed", StringComparison.Ordinal))
        {
            advisories.Add(new EeatAdvisory
            {
                Code = "freshness_signal",
                ActionText = "Add a visible last-updated or reviewed date for YMYL topics.",
            });
        }

        return advisories;
    }

    private static bool HasOutboundCitation(string html) =>
        html.Contains("href=\"http", StringComparison.OrdinalIgnoreCase)
        && !html.Contains("geekatyourspot", StringComparison.OrdinalIgnoreCase);

    private static int CountOccurrences(string haystack, string needle) =>
        haystack.Split(needle, StringSplitOptions.None).Length - 1;
}
