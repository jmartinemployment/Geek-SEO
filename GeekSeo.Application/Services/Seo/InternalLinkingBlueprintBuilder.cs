using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class InternalLinkingBlueprintBuilder
{
    public static InternalLinkingBlueprint Build(IReadOnlyList<TopicalMapTopic> topics)
    {
        var sequences = BuildContentSequence(topics);
        var linkGraph = BuildLinkGraph(topics);

        return new InternalLinkingBlueprint
        {
            Sequences = sequences,
            LinkGraph = linkGraph,
        };
    }

    private static IReadOnlyList<ContentSequenceItem> BuildContentSequence(IReadOnlyList<TopicalMapTopic> topics)
    {
        var sequences = new List<ContentSequenceItem>();
        var order = 1;

        // Order: Pillar → Cluster → Article
        foreach (var tier in new[] { TopicalTier.Pillar, TopicalTier.Cluster, TopicalTier.Article })
        {
            var tierTopics = topics
                .Where(t => t.Tier == tier)
                .OrderByDescending(t => t.SearchVolume ?? 0)
                .ThenBy(t => t.KeywordDifficulty ?? 100)
                .ToList();

            foreach (var topic in tierTopics)
            {
                var reason = tier switch
                {
                    TopicalTier.Pillar => "Pillar — publish first to establish topical authority",
                    TopicalTier.Cluster => $"High-volume cluster ({topic.SearchVolume:N0}/mo) — foundational sub-topic",
                    TopicalTier.Article => "Long-tail article — publish after parent cluster is indexed",
                    _ => null,
                };

                sequences.Add(new ContentSequenceItem
                {
                    Order = order++,
                    TopicId = topic.Name, // Use topic name as ID (no UUID in model)
                    TopicName = topic.Name,
                    Tier = tier,
                    Reason = reason,
                });
            }
        }

        return sequences;
    }

    private static IReadOnlyList<LinkGraphEdge> BuildLinkGraph(IReadOnlyList<TopicalMapTopic> topics)
    {
        var edges = new List<LinkGraphEdge>();
        var topicsByName = topics.ToDictionary(t => t.Name);

        // Article → parent Cluster
        var articles = topics.Where(t => t.Tier == TopicalTier.Article).ToList();
        foreach (var article in articles)
        {
            if (article.ParentClusterId is not null && topicsByName.TryGetValue(article.ParentClusterId, out var cluster))
            {
                edges.Add(new LinkGraphEdge
                {
                    SourceTopicId = article.Name,
                    TargetTopicId = cluster.Name,
                    AnchorText = article.MainKeyword ?? article.Name,
                    Priority = "high",
                });
            }
        }

        // Cluster → parent Pillar
        var clusters = topics.Where(t => t.Tier == TopicalTier.Cluster).ToList();
        foreach (var cluster in clusters)
        {
            if (cluster.PillarId is not null && topicsByName.TryGetValue(cluster.PillarId, out var pillar))
            {
                edges.Add(new LinkGraphEdge
                {
                    SourceTopicId = cluster.Name,
                    TargetTopicId = pillar.Name,
                    AnchorText = cluster.MainKeyword ?? cluster.Name,
                    Priority = "high",
                });
            }
        }

        // Pillar → all child Clusters
        var pillars = topics.Where(t => t.Tier == TopicalTier.Pillar).ToList();
        foreach (var pillar in pillars)
        {
            var childClusters = clusters.Where(c => c.PillarId == pillar.Name).ToList();
            foreach (var cluster in childClusters)
            {
                edges.Add(new LinkGraphEdge
                {
                    SourceTopicId = pillar.Name,
                    TargetTopicId = cluster.Name,
                    AnchorText = cluster.Name,
                    Priority = "medium",
                });
            }
        }

        // Cross-cluster links: clusters sharing ≥2 entity terms
        for (int i = 0; i < clusters.Count; i++)
        {
            for (int j = i + 1; j < clusters.Count; j++)
            {
                var c1 = clusters[i];
                var c2 = clusters[j];

                var c1Entities = new HashSet<string>(c1.EntityGaps);
                var c2Entities = new HashSet<string>(c2.EntityGaps);

                var shared = c1Entities.Intersect(c2Entities).Count();
                if (shared >= 2)
                {
                    var sharedTerm = c1Entities.Intersect(c2Entities).FirstOrDefault() ?? "related topic";

                    // Bidirectional
                    edges.Add(new LinkGraphEdge
                    {
                        SourceTopicId = c1.Name,
                        TargetTopicId = c2.Name,
                        AnchorText = sharedTerm,
                        Priority = "low",
                    });

                    edges.Add(new LinkGraphEdge
                    {
                        SourceTopicId = c2.Name,
                        TargetTopicId = c1.Name,
                        AnchorText = sharedTerm,
                        Priority = "low",
                    });
                }
            }
        }

        return edges;
    }
}
