using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class SerpTermMatrixBuilder
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "is", "are", "was", "were", "be", "been", "this", "that", "these", "those", "your", "our", "their",
    };

    public static SerpTermMatrix Build(IReadOnlyList<DeepSerpOrganic> organic)
    {
        var termCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in organic)
        {
            foreach (var term in Tokenize($"{row.Title} {row.Snippet}"))
            {
                termCounts.TryGetValue(term, out var count);
                termCounts[term] = count + 1;
            }
        }

        var terms = termCounts
            .OrderByDescending(kv => kv.Value)
            .Take(25)
            .Select(kv => kv.Key)
            .ToList();

        var rows = organic.Select(o =>
        {
            var text = $"{o.Title} {o.Snippet}".ToLowerInvariant();
            var counts = terms.Select(t => CountTerm(text, t)).ToList();
            return new SerpTermMatrixRow
            {
                Position = o.Position,
                Url = o.Url,
                Title = o.Title,
                Counts = counts,
            };
        }).ToList();

        return new SerpTermMatrix { Terms = terms, Rows = rows };
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        foreach (var raw in text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var word = raw.Trim('.', ',', ';', ':', '"', '\'', '(', ')', '[', ']');
            if (word.Length >= 3 && !StopWords.Contains(word))
                yield return word;
        }
    }

    private static int CountTerm(string text, string term) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(w => string.Equals(w.Trim('.', ',', ';', ':', '"', '\'', '(', ')', '[', ']'), term, StringComparison.OrdinalIgnoreCase));
}
