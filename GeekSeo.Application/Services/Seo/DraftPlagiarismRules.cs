using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class DraftPlagiarismRules
{
    public const int LongSubstringWordThreshold = 8;

    public static PlagiarismRuleReport Evaluate(string html, WritingResearchContext research)
    {
        var plain = HtmlTextUtility.StripHtml(html);
        var headings = ExtractHeadings(html);
        var sourcePhrases = CollectSourcePhrases(research);
        var brandStems = CollectBrandStems(research);
        var results = new List<PlagiarismRuleResult>();

        foreach (var heading in headings)
        {
            var normalizedHeading = Normalize(heading);
            foreach (var phrase in sourcePhrases.Titles)
            {
                if (string.Equals(normalizedHeading, Normalize(phrase.Text), StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new PlagiarismRuleResult(
                        "verbatim_title",
                        false,
                        $"Heading matches SERP title: \"{phrase.Text}\"",
                        phrase.Text));
                    break;
                }
            }

            if (brandStems.Any(stem => heading.Contains(stem, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new PlagiarismRuleResult(
                    "brand_heading",
                    false,
                    $"Heading contains competitor or SERP brand token: \"{heading}\"",
                    heading));
            }
        }

        foreach (var sentence in SplitSentences(plain))
        {
            if (WordRegex().Matches(sentence).Count < LongSubstringWordThreshold)
                continue;

            foreach (var phrase in sourcePhrases.Snippets)
            {
                if (ContainsLongSharedSubstring(sentence, phrase.Text))
                {
                    results.Add(new PlagiarismRuleResult(
                        "long_substring",
                        false,
                        $"Sentence shares {LongSubstringWordThreshold}+ words with SERP snippet",
                        phrase.Text));
                    break;
                }
            }
        }

        foreach (Match match in HrefRegex().Matches(html))
        {
            var href = match.Groups[1].Value.Trim();
            if (!IsCompetitorUrl(href, research))
                continue;

            results.Add(new PlagiarismRuleResult(
                "competitor_link",
                false,
                $"Body links to competitor URL: {href}",
                href));
        }

        return new PlagiarismRuleReport(results);
    }

    public static bool PassesAllRules(string html, WritingResearchContext research) =>
        Evaluate(html, research).Passed;

    private static bool ContainsLongSharedSubstring(string sentence, string source)
    {
        var sentenceWords = WordRegex().Matches(Normalize(sentence)).Select(m => m.Value.ToLowerInvariant()).ToList();
        var sourceWords = WordRegex().Matches(Normalize(source)).Select(m => m.Value.ToLowerInvariant()).ToList();
        if (sentenceWords.Count < LongSubstringWordThreshold || sourceWords.Count < LongSubstringWordThreshold)
            return false;

        for (var i = 0; i <= sentenceWords.Count - LongSubstringWordThreshold; i++)
        {
            var slice = string.Join(' ', sentenceWords.Skip(i).Take(LongSubstringWordThreshold));
            var sourceSlice = string.Join(' ', sourceWords.Take(LongSubstringWordThreshold));
            if (slice == sourceSlice)
                return true;
        }

        return false;
    }

    private static bool IsCompetitorUrl(string href, WritingResearchContext research)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            return false;

        if (AuthoritativeCitationRules.IsAuthoritativeCitationUrl(href))
            return false;

        var host = uri.Host.ToLowerInvariant();
        return research.Competitors.Any(c => Uri.TryCreate(c.Url, UriKind.Absolute, out var cu)
                && string.Equals(cu.Host, host, StringComparison.OrdinalIgnoreCase))
            || research.Organic.Any(o => Uri.TryCreate(o.Url, UriKind.Absolute, out var ou)
                && string.Equals(ou.Host, host, StringComparison.OrdinalIgnoreCase));
    }

    private static SourcePhrases CollectSourcePhrases(WritingResearchContext research)
    {
        var titles = new List<SourcePhrase>();
        var snippets = new List<SourcePhrase>();

        void AddTitle(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                titles.Add(new SourcePhrase(text.Trim()));
        }

        void AddSnippet(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text) && text.Trim().Length >= 20)
                snippets.Add(new SourcePhrase(text.Trim()));
        }

        foreach (var competitor in research.Competitors)
        {
            AddTitle(competitor.H1);
            foreach (var heading in competitor.Headings)
                AddTitle(heading.Text);
        }

        foreach (var organic in research.Organic)
        {
            AddTitle(organic.Title);
            AddSnippet(organic.Snippet);
        }

        foreach (var paa in research.PeopleAlsoAsk)
            AddSnippet(paa.SerpAnswerPreview);

        return new SourcePhrases(titles, snippets);
    }

    private static HashSet<string> CollectBrandStems(WritingResearchContext research)
    {
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var competitor in research.Competitors)
        {
            if (Uri.TryCreate(competitor.Url, UriKind.Absolute, out var uri))
            {
                var label = uri.Host.Split('.')[0];
                if (label.Length >= 4)
                    stems.Add(label);
            }
        }

        return stems;
    }

    private static List<string> ExtractHeadings(string html)
    {
        var headings = new List<string>();
        foreach (Match match in HeadingRegex().Matches(html))
            headings.Add(HtmlTextUtility.StripHtml(match.Groups[1].Value));

        return headings;
    }

    private static IEnumerable<string> SplitSentences(string plain) =>
        plain.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Normalize(string value) =>
        WhitespaceRegex().Replace(value.Trim(), " ");

    [GeneratedRegex(@"<h[23][^>]*>(.*?)</h[23]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"href=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"\b[\w']+\b")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private readonly record struct SourcePhrase(string Text);

    private readonly record struct SourcePhrases(
        IReadOnlyList<SourcePhrase> Titles,
        IReadOnlyList<SourcePhrase> Snippets);
}

public sealed record PlagiarismRuleResult(
    string RuleId,
    bool Passed,
    string Detail,
    string? MatchedSource);

public sealed record PlagiarismRuleReport(IReadOnlyList<PlagiarismRuleResult> Results)
{
    public bool Passed => Results.Count == 0 || Results.All(r => r.Passed);

    public IReadOnlyList<PlagiarismRuleResult> Failures =>
        Results.Where(r => !r.Passed).ToList();
}
