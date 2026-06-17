using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

internal static class SiteBusinessProfileBuilder
{
    internal static SiteBusinessProfile Build(SchemaOrgData schema, HomepageHeadings? headings = null)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var phrase in schema.KnowsAboutTopics
                     .Concat(schema.OfferCatalogTopics)
                     .Concat(schema.ServiceNames))
        {
            AddPhraseTokens(tokens, phrase);
        }

        AddPhraseTokens(tokens, schema.Description);
        AddPhraseTokens(tokens, schema.BrandName);
        AddPhraseTokens(tokens, headings?.Title);

        return new SiteBusinessProfile(tokens);
    }

    private static void AddPhraseTokens(ISet<string> tokens, string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return;

        foreach (var token in CompetitorRelevanceFilter.Tokenize(phrase))
            tokens.Add(token);
    }
}
