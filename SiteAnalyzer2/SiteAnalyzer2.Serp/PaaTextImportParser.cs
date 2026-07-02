using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

/// <summary>Import curated PAA question lists (one question per line) for the manual <c>paa</c> lane.</summary>
public static class PaaTextImportParser
{
    public static bool LooksLikePaaTextList(string content)
    {
        if (GoogleSerpHtmlParser.LooksLikeSerpPage(content))
            return false;

        return content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(line => line.Length >= 8 && !line.StartsWith('#'))
            >= 1;
    }

    public static SerpLivePageParseResult Parse(string text, string keyword)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length < 8 || line.StartsWith('#'))
                continue;

            if (seen.Add(line))
                questions.Add(line);
        }

        if (questions.Count == 0)
        {
            throw new InvalidOperationException(
                "PAA text file has no questions — use one question per line.");
        }

        var queries = questions
            .Select((question, index) => new SerpParsedRelatedQuery(
                index + 1,
                question,
                SerpRelatedQueryType.PeopleAlsoAsk))
            .ToList();

        var item = new SerpParsedItem(
            SerpItemTypes.PeopleAlsoAsk,
            RankGroup: 1,
            RankAbsolute: 1,
            Page: 1,
            RelatedQueries: queries);

        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? "unknown keyword" : keyword.Trim();

        return new SerpLivePageParseResult(
            normalizedKeyword,
            LocationCode: 2840,
            LanguageCode: "en",
            Device: "desktop",
            Os: "windows",
            Depth: 1,
            SeDomain: "google.com",
            CheckUrl: string.Empty,
            CapturedAtUtc: DateTime.UtcNow,
            SeResultsCount: null,
            PagesCount: 1,
            ItemTypes: [SerpItemTypes.PeopleAlsoAsk],
            LocalPackPresent: false,
            ShoppingResultsPresent: false,
            Items: [item]);
    }
}
