using SiteAnalyzer2.Domain;

namespace SiteAnalyzer2.Services.Integrations;

internal static class CitationLaneValidationMessages
{
    internal static string? WrongDomainHint(string lane, IReadOnlyList<string> domains)
    {
        if (!string.Equals(lane, SerpResearchLanes.Wiki, StringComparison.OrdinalIgnoreCase)
            || domains.Count == 0)
        {
            return null;
        }

        if (domains.Any(d => CitationLaneDomainRules.IsNonWikipediaWikiTld(d)))
        {
            return " Found .wiki sites (custom TLD) — those are not Wikipedia."
                + " Use Google site:en.wikipedia.org and save Webpage, HTML only.";
        }

        if (domains.All(d => !CitationLaneDomainRules.IsWikipediaHost(d)))
        {
            return " No en.wikipedia.org URLs in this SERP."
                + " Generic .wiki domains and other sites do not count for the wiki lane.";
        }

        return null;
    }
}
