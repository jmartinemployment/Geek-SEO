using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class EntityGapAnalyzer(ISerpDeepCacheRepository serpCacheRepo)
{
    private static readonly string[] Stopwords = new[]
    {
        "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is",
        "it", "its", "of", "on", "or", "that", "the", "to", "was", "will", "with", "you",
        "about", "this", "which", "your", "their", "what", "when", "where", "why", "how"
    };

    public async Task<IReadOnlyList<TopicalMapTopic>> AnalyzeAsync(
        IReadOnlyList<TopicalMapTopic> topics,
        IReadOnlyList<string> projectQueries,
        string location,
        CancellationToken ct)
    {
        var projectEntities = ExtractProjectEntities(projectQueries);
        var enriched = new List<TopicalMapTopic>();

        foreach (var topic in topics)
        {
            if (string.IsNullOrWhiteSpace(topic.MainKeyword) || topic.CompetitorDomains.Count == 0)
            {
                enriched.Add(topic with { EntityGaps = [], EntityCoverage = 1m });
                continue;
            }

            var serpCache = await serpCacheRepo.GetAsync(topic.MainKeyword, location, 10, ct);
            if (!serpCache.IsSuccess || serpCache.Value?.ResultsJson == null)
            {
                enriched.Add(topic with { EntityGaps = [], EntityCoverage = 1m });
                continue;
            }

            var competitorEntities = ExtractCompetitorEntities(serpCache.Value.ResultsJson);
            var gaps = FindEntityGaps(competitorEntities, projectEntities);
            var coverage = CalculateEntityCoverage(competitorEntities, projectEntities);

            enriched.Add(topic with
            {
                EntityGaps = gaps,
                EntityCoverage = coverage,
            });
        }

        return enriched;
    }

    private static HashSet<string> ExtractProjectEntities(IReadOnlyList<string> queries)
    {
        var entities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in queries)
        {
            var ngrams = ExtractNgrams(query, 1, 3);
            foreach (var ngram in ngrams)
                entities.Add(ngram);
        }
        return entities;
    }

    private static Dictionary<string, int> ExtractCompetitorEntities(string resultsJson)
    {
        var entities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var results = System.Text.Json.JsonDocument.Parse(resultsJson);
            var root = results.RootElement;

            if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.TryGetProperty("title", out var titleEl))
                    {
                        var title = titleEl.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            var ngrams = ExtractNgrams(title, 1, 3);
                            foreach (var ngram in ngrams)
                            {
                                if (entities.ContainsKey(ngram))
                                    entities[ngram]++;
                                else
                                    entities[ngram] = 1;
                            }
                        }
                    }

                    if (item.TryGetProperty("snippet", out var snippetEl))
                    {
                        var snippet = snippetEl.GetString();
                        if (!string.IsNullOrWhiteSpace(snippet))
                        {
                            var ngrams = ExtractNgrams(snippet, 1, 3);
                            foreach (var ngram in ngrams)
                            {
                                if (entities.ContainsKey(ngram))
                                    entities[ngram]++;
                                else
                                    entities[ngram] = 1;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            return entities;
        }

        return entities;
    }

    private static IReadOnlyList<string> FindEntityGaps(
        Dictionary<string, int> competitorEntities,
        HashSet<string> projectEntities)
    {
        return competitorEntities
            .Where(x => x.Value >= 3 && !projectEntities.Contains(x.Key))
            .OrderByDescending(x => x.Value)
            .Take(20)
            .Select(x => x.Key)
            .ToList();
    }

    private static decimal CalculateEntityCoverage(
        Dictionary<string, int> competitorEntities,
        HashSet<string> projectEntities)
    {
        if (competitorEntities.Count == 0)
            return 1m;

        var covered = competitorEntities.Keys.Count(e => projectEntities.Contains(e));
        return covered == 0 ? 0m : (decimal)covered / competitorEntities.Count;
    }

    private static IReadOnlyList<string> ExtractNgrams(string text, int minGrams, int maxGrams)
    {
        var tokens = Tokenize(text);
        var ngrams = new List<string>();

        for (int n = minGrams; n <= maxGrams && n <= tokens.Count; n++)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var ngram = string.Join(" ", tokens.Skip(i).Take(n));
                if (!string.IsNullOrWhiteSpace(ngram) && ngram.Length > 1)
                    ngrams.Add(ngram);
            }
        }

        return ngrams.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> Tokenize(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(t => !string.IsNullOrWhiteSpace(t) && !Stopwords.Contains(t))
            .ToList();
    }
}
