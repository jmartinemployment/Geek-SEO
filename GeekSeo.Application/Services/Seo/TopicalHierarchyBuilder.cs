namespace GeekSeo.Application.Services.Seo;

using GeekSeo.Application.Models.Seo;

public interface ITopicalHierarchyBuilder
{
    /// <summary>Assign tier and link topics to pillars/clusters.</summary>
    Task<TopicalMapTopic[]> AssignTiersAsync(TopicalMapTopic[] topics, CancellationToken ct = default);
}

public class TopicalHierarchyBuilder : ITopicalHierarchyBuilder
{
    public Task<TopicalMapTopic[]> AssignTiersAsync(TopicalMapTopic[] topics, CancellationToken ct = default)
    {
        var assignedTopics = new List<TopicalMapTopic>();
        var pillarMap = new Dictionary<string, (int Index, TopicalMapTopic Topic)>();
        var clusterMap = new Dictionary<string, (int Index, TopicalMapTopic Topic)>();

        // First pass: classify all topics into tiers
        var tieredTopics = topics.Select(t => ClassifyTier(t)).ToList();

        // Group pillars and clusters by keyword similarity for hierarchy linking
        var pillars = tieredTopics.Where(t => t.Tier == TopicalTier.Pillar).ToList();
        var clusters = tieredTopics.Where(t => t.Tier == TopicalTier.Cluster).ToList();
        var articles = tieredTopics.Where(t => t.Tier == TopicalTier.Article).ToList();

        // Build pillar index
        for (int i = 0; i < pillars.Count; i++)
        {
            pillarMap[pillars[i].Name] = (i, pillars[i]);
        }

        // Build cluster index and link clusters to pillars
        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            var linkedPillar = FindParentPillar(cluster, pillars);
            var linkedCluster = linkedPillar with { PillarId = linkedPillar.Name };
            clusterMap[linkedCluster.Name] = (i, linkedCluster);
            clusters[i] = linkedCluster;
        }

        // Link articles to clusters
        for (int i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            var linkedCluster = FindParentCluster(article, clusters);
            articles[i] = article with
            {
                ParentClusterId = linkedCluster?.Name,
                PillarId = linkedCluster?.PillarId
            };
        }

        // Combine back and sort by tier + name
        assignedTopics.AddRange(pillars);
        assignedTopics.AddRange(clusters);
        assignedTopics.AddRange(articles);

        return Task.FromResult(assignedTopics.OrderBy(t => (int)t.Tier).ThenBy(t => t.Name).ToArray());
    }

    private TopicalMapTopic ClassifyTier(TopicalMapTopic topic)
    {
        var wordCount = topic.MainKeyword?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 1;
        var volume = topic.SearchVolume ?? 0;

        var tier = (volume, wordCount) switch
        {
            (> 1000, <= 2) => TopicalTier.Pillar,
            (>= 100 and <= 1000, >= 2 and <= 3) => TopicalTier.Cluster,
            _ => TopicalTier.Article
        };

        return topic with { Tier = tier };
    }

    private TopicalMapTopic FindParentPillar(TopicalMapTopic cluster, List<TopicalMapTopic> pillars)
    {
        if (pillars.Count == 0)
            return cluster;

        var clusterWords = cluster.MainKeyword?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var clusterTokens = new HashSet<string>(clusterWords, StringComparer.OrdinalIgnoreCase);

        var bestPillar = pillars[0];
        var bestScore = 0;

        foreach (var pillar in pillars)
        {
            var pillarWords = pillar.MainKeyword?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var pillarTokens = new HashSet<string>(pillarWords, StringComparer.OrdinalIgnoreCase);
            var overlap = clusterTokens.Intersect(pillarTokens, StringComparer.OrdinalIgnoreCase).Count();

            if (overlap > bestScore)
            {
                bestScore = overlap;
                bestPillar = pillar;
            }
        }

        return bestPillar;
    }

    private TopicalMapTopic? FindParentCluster(TopicalMapTopic article, List<TopicalMapTopic> clusters)
    {
        if (clusters.Count == 0)
            return null;

        var articleWords = article.MainKeyword?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var articleTokens = new HashSet<string>(articleWords, StringComparer.OrdinalIgnoreCase);

        var bestCluster = clusters[0];
        var bestScore = 0;

        foreach (var cluster in clusters)
        {
            var clusterWords = cluster.MainKeyword?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var clusterTokens = new HashSet<string>(clusterWords, StringComparer.OrdinalIgnoreCase);
            var overlap = articleTokens.Intersect(clusterTokens, StringComparer.OrdinalIgnoreCase).Count();

            if (overlap > bestScore)
            {
                bestScore = overlap;
                bestCluster = cluster;
            }
        }

        return bestCluster;
    }

    public static IReadOnlyList<TopicalMapTopic> DeduplicateTopics(IReadOnlyList<TopicalMapTopic> topics)
    {
        var result = new List<TopicalMapTopic>(topics);
        var marked = new HashSet<int>();

        for (int i = 0; i < result.Count; i++)
        {
            if (marked.Contains(i))
                continue;

            for (int j = i + 1; j < result.Count; j++)
            {
                if (marked.Contains(j))
                    continue;

                var similarity = JaccardSimilarity(result[i], result[j]);

                if (similarity >= 0.70m)
                {
                    var highPriority = (result[i].PriorityScore >= result[j].PriorityScore) ? i : j;
                    var lowPriority = highPriority == i ? j : i;

                    result[lowPriority] = result[lowPriority] with
                    {
                        IsDuplicate = true,
                        DuplicateOf = result[highPriority].Name,
                    };
                    marked.Add(lowPriority);
                }
                else if (similarity >= 0.55m && result[i].Coverage == "covered" && result[j].Coverage == "covered")
                {
                    result[j] = result[j] with { IsDuplicate = true, DuplicateOf = result[i].Name };
                    marked.Add(j);
                }
            }
        }

        return result;
    }

    private static decimal JaccardSimilarity(TopicalMapTopic a, TopicalMapTopic b)
    {
        var aTokens = TokenizeKeyword(a.Name);
        var bTokens = TokenizeKeyword(b.Name);

        var intersection = aTokens.Intersect(bTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = aTokens.Union(bTokens, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? 0 : (decimal)intersection / union;
    }

    private static HashSet<string> TokenizeKeyword(string keyword)
    {
        return new HashSet<string>(
            keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);
    }
}
