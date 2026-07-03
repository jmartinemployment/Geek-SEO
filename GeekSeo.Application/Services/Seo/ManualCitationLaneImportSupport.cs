using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Query hints and validation copy for manual citation lane HTML imports.
/// </summary>
public static class ManualCitationLaneImportSupport
{
    private const string Junk = "-template -pdf -generator -reddit -quora -course -syllabus";

    public static string QueryHint(string lane, string? keyword)
    {
        var phrase = string.IsNullOrWhiteSpace(keyword)
            ? "\"your keyword\""
            : $"\"{keyword.Trim().Replace("\"", string.Empty, StringComparison.Ordinal)}\"";

        return lane.ToLowerInvariant() switch
        {
            SerpResearchLanes.Wiki =>
                $"Google: {phrase} site:en.wikipedia.org {Junk} — results must be en.wikipedia.org (not .wiki sites)",
            SerpResearchLanes.Gov =>
                $"Google: {phrase} (site:nist.gov OR site:ftc.gov OR site:usa.gov OR site:cdc.gov OR site:nih.gov) {Junk}",
            SerpResearchLanes.Edu =>
                $"Google: {phrase} site:edu {Junk}",
            _ => string.Empty,
        };
    }

    public static string? WrongDomainHint(string lane, IReadOnlyList<string> domains)
    {
        if (!string.Equals(lane, SerpResearchLanes.Wiki, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(lane, SerpResearchLanes.Gov, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(lane, SerpResearchLanes.Edu, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (domains.Any(d => CitationLaneHostRules.IsNonWikipediaWikiTld(d)))
        {
            return " Found .wiki sites (custom TLD) — those are not Wikipedia."
                + " Use Google site:en.wikipedia.org and save Webpage, HTML only.";
        }

        if (string.Equals(lane, SerpResearchLanes.Wiki, StringComparison.OrdinalIgnoreCase)
            && domains.All(d => !CitationLaneHostRules.IsWikipediaHost(d)))
        {
            return " No en.wikipedia.org URLs in this SERP."
                + " Generic .wiki domains and other sites do not count for the wiki lane.";
        }

        return null;
    }

    public static string ImportFailureMessage(
        string lane,
        int organicCount,
        IReadOnlyList<string> domains,
        string? keyword)
    {
        var domainHint = domains.Count > 0
            ? $" Parsed {organicCount} organic result(s); domains seen: {string.Join(", ", domains)}."
            : $" Parsed {organicCount} organic result(s).";

        var wrongDomainHint = WrongDomainHint(lane, domains) ?? string.Empty;
        var queryHint = string.IsNullOrWhiteSpace(QueryHint(lane, keyword))
            ? string.Empty
            : $" Re-run the Google search using: {QueryHint(lane, keyword)} — then save Webpage, HTML only.";

        return $"Lane '{lane}' produced 0 citation-eligible URLs after domain validation.{domainHint}{wrongDomainHint}{queryHint}";
    }
}
