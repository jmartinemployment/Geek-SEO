using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

/// <summary>Merges multiple PAA lane uploads (txt lists or saved SERP HTML) into one parse result.</summary>
public static class PaaLaneImportComposer
{
    public static SerpLivePageParseResult MergeContents(
        IEnumerable<PaaLaneImportFile> files,
        string keyword)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.Content))
                continue;

            var parsed = PaaLaneContentParser.Parse(file.Content, keyword, file.FileName);
            foreach (var question in ExtractQuestions(parsed))
            {
                if (seen.Add(question))
                    questions.Add(question);
            }
        }

        if (questions.Count == 0)
        {
            throw new InvalidOperationException(
                "PAA import produced 0 People Also Ask questions across all uploaded files.");
        }

        return ApplyKeywordRelevance(keyword, questions);
    }

    public static SerpLivePageParseResult ApplyKeywordRelevance(string keyword, IReadOnlyList<string> questions)
    {
        var relevant = PaaQuestionRelevanceFilter.Filter(keyword, questions);
        if (relevant.Count == 0)
        {
            throw new InvalidOperationException(
                $"PAA import produced 0 keyword-relevant questions for '{keyword.Trim()}' — all {questions.Count} question(s) were off-topic or blocked.");
        }

        return PaaTextImportParser.BuildFromQuestions(keyword, relevant);
    }

    public static SerpLivePageParseResult ApplyKeywordRelevance(SerpLivePageParseResult parsed, string keyword)
    {
        var questions = ExtractQuestions(parsed);
        if (questions.Count == 0)
            return parsed;

        return ApplyKeywordRelevance(keyword, questions);
    }

    public static IReadOnlyList<string> ExtractQuestions(SerpLivePageParseResult parsed)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in parsed.Items)
        {
            if (item.RelatedQueries is not { Count: > 0 })
                continue;

            foreach (var query in item.RelatedQueries.OrderBy(q => q.Sequence))
            {
                if (string.IsNullOrWhiteSpace(query.QueryText))
                    continue;

                var trimmed = query.QueryText.Trim();
                if (query.QueryType != SerpRelatedQueryType.PeopleAlsoAsk
                    && !trimmed.Contains('?', StringComparison.Ordinal))
                {
                    continue;
                }

                if (seen.Add(trimmed))
                    questions.Add(trimmed);
            }
        }

        return questions;
    }
}

public sealed record PaaLaneImportFile(string? FileName, string Content);

/// <summary>Parses a single PAA lane file (plain-text list or saved Google HTML).</summary>
public static class PaaLaneContentParser
{
    public static SerpLivePageParseResult Parse(string content, string keyword, string? fileName = null)
    {
        if (LooksLikePlainTextPaa(fileName, content)
            && PaaTextImportParser.LooksLikePaaTextList(content))
        {
            return PaaTextImportParser.Parse(content, keyword);
        }

        if (!GoogleSerpHtmlParser.LooksLikeSerpPage(content))
        {
            throw new InvalidOperationException(
                "PAA lane accepts saved Google SERP HTML or a plain-text file with one question per line.");
        }

        return GoogleSerpHtmlParser.ParseLivePage(content, keywordOverride: keyword);
    }

    private static bool LooksLikePlainTextPaa(string? fileName, string content) =>
        (fileName ?? string.Empty).EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
        || (!GoogleSerpHtmlParser.LooksLikeSerpPage(content)
            && PaaTextImportParser.LooksLikePaaTextList(content));
}
