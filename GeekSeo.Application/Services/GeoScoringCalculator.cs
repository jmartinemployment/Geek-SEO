using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class GeoScoringCalculator
{
    public sealed record GeoScoreResult
    {
        public required int TotalScore { get; init; }
        public required string Grade { get; init; }
        public required object Components { get; init; }
        public required IReadOnlyList<ScoreSuggestion> Suggestions { get; init; }
    }

    public static GeoScoreResult Calculate(string plainText, string contentHtml, int wordCount, int benchmarkWordCount)
    {
        var authority = ScoreAuthority(plainText, contentHtml);
        var readability = ScoreGeoReadability(plainText);
        var structure = ScoreStructure(contentHtml);
        var citations = ScoreCitations(contentHtml);
        var depth = ScoreDepth(wordCount, benchmarkWordCount);

        var total = authority + readability + structure + citations + depth;
        var suggestions = BuildSuggestions(authority, readability, structure, citations, depth);

        return new GeoScoreResult
        {
            TotalScore = total,
            Grade = ScoreToGrade(total),
            Components = new
            {
                authority,
                readability,
                structure,
                citations,
                depth,
            },
            Suggestions = suggestions,
        };
    }

    private static int ScoreAuthority(string plainText, string contentHtml)
    {
        var score = 0;
        var lower = plainText.ToLowerInvariant();
        if (lower.Contains("according to", StringComparison.Ordinal) || lower.Contains("research shows", StringComparison.Ordinal))
            score += 6;
        if (lower.Contains("certified", StringComparison.Ordinal) || lower.Contains("licensed", StringComparison.Ordinal))
            score += 4;
        if (contentHtml.Contains("schema.org", StringComparison.OrdinalIgnoreCase))
            score += 6;
        if (RegexQuoteCount(plainText) >= 1)
            score += 4;
        return Math.Min(20, score);
    }

    private static int ScoreGeoReadability(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return 0;

        var words = plainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var sentences = Math.Max(1, plainText.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries).Length);
        var avg = words / (double)sentences;

        return avg switch
        {
            >= 8 and <= 18 => 20,
            >= 6 and <= 22 => 14,
            _ => 8,
        };
    }

    private static int ScoreStructure(string html)
    {
        var score = 0;
        var h2 = CountOccurrences(html, "<h2");
        var h3 = CountOccurrences(html, "<h3");
        if (h2 >= 2)
            score += 8;
        if (h3 >= 1)
            score += 4;
        if (html.Contains("<ul", StringComparison.OrdinalIgnoreCase) || html.Contains("<ol", StringComparison.OrdinalIgnoreCase))
            score += 4;
        if (html.Contains("?", StringComparison.Ordinal))
            score += 4;
        return Math.Min(20, score);
    }

    private static int ScoreCitations(string html)
    {
        var links = CountOccurrences(html, "<a ");
        return links switch
        {
            >= 3 => 20,
            2 => 14,
            1 => 8,
            _ => 0,
        };
    }

    private static int ScoreDepth(int wordCount, int benchmarkWordCount)
    {
        if (benchmarkWordCount <= 0)
            benchmarkWordCount = 1200;
        var ratio = wordCount / (double)benchmarkWordCount;
        return ratio switch
        {
            >= 0.9 => 20,
            >= 0.7 => 14,
            >= 0.5 => 8,
            _ => 4,
        };
    }

    private static List<ScoreSuggestion> BuildSuggestions(int authority, int readability, int structure, int citations, int depth)
    {
        var list = new List<ScoreSuggestion>();
        if (authority < 14)
        {
            list.Add(new ScoreSuggestion
            {
                Component = "geo",
                PointValue = 20 - authority,
                ActionText = "Add expert quotes, credentials, or Article schema to improve AI citation trust.",
            });
        }
        if (readability < 14)
        {
            list.Add(new ScoreSuggestion
            {
                Component = "geo",
                PointValue = 20 - readability,
                ActionText = "Use shorter sentences and direct definitions so AI models can extract clear answers.",
            });
        }
        if (structure < 14)
        {
            list.Add(new ScoreSuggestion
            {
                Component = "geo",
                PointValue = 20 - structure,
                ActionText = "Add FAQ-style H2/H3 headings and bullet lists for snippet-friendly structure.",
            });
        }
        if (citations < 10)
        {
            list.Add(new ScoreSuggestion
            {
                Component = "geo",
                PointValue = 20 - citations,
                ActionText = "Link to 2–3 authoritative external sources to strengthen citation signals.",
            });
        }
        if (depth < 14)
        {
            list.Add(new ScoreSuggestion
            {
                Component = "geo",
                PointValue = 20 - depth,
                ActionText = "Expand subtopic coverage to match competitor depth for AI answer completeness.",
            });
        }

        return list.OrderByDescending(s => s.PointValue).Take(5).ToList();
    }

    private static int RegexQuoteCount(string text) =>
        text.Split('"', StringSplitOptions.RemoveEmptyEntries).Length > 1 ? 1 : 0;

    private static int CountOccurrences(string haystack, string needle) =>
        haystack.Split(needle, StringSplitOptions.None).Length - 1;

    private static string ScoreToGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F",
    };
}
