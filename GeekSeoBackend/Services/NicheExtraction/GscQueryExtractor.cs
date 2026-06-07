using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeoBackend.Models;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Tier-3 owner signal: GSC query clusters mapped to fused topic candidates (Phase D).
/// </summary>
public sealed class GscQueryExtractor(
    IGoogleDataService googleData,
    IGoogleIntegrationRepository integrations)
{
    public const int DefaultRowLimit = 2500;
    public const int MinImpressionsForMatch = 5;
    private const int GscTimeoutMs = 30_000;

    public async Task<GscOwnerOverlay> ExtractAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(GscTimeoutMs);

        var connection = await integrations.GetGscConnectionAsync(projectId, userId, timeoutCts.Token);
        if (!connection.IsSuccess)
        {
            return GscOwnerOverlay.Unavailable(
                connected: false,
                reason: connection.Error ?? "Could not load GSC connection.");
        }

        if (connection.Value is null)
        {
            return GscOwnerOverlay.Unavailable(
                connected: false,
                reason: "Google Search Console is not connected for this project.");
        }

        GoogleRankingsResponse rankings;
        try
        {
            var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
            var start = end.AddDays(-89);
            rankings = await googleData.GetRankingsAsync(
                userId,
                projectId,
                start,
                end,
                DefaultRowLimit,
                timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return GscOwnerOverlay.Unavailable(connected: true, reason: "GSC query timed out after 30s");
        }
        catch (GoogleIntegrationException ex)
        {
            return GscOwnerOverlay.Unavailable(connected: true, reason: ex.Message);
        }

        var rows = rankings.Rows
            .Select(r => new GscQueryRow(r.Query, r.Page, r.Impressions, r.Clicks, r.Position))
            .ToList();
        if (rows.Count == 0)
        {
            return new GscOwnerOverlay(
                Connected: true,
                Skipped: false,
                SkipReason: null,
                QueryRowCount: 0,
                Clusters: [],
                Matches: []);
        }

        var clusters = TopicClusteringService.ClusterGscQueries(rows);
        var matches = new List<GscPillarMatch>();
        foreach (var cluster in clusters)
        {
            if (cluster.TotalImpressions < MinImpressionsForMatch)
                continue;

            matches.Add(new GscPillarMatch(
                cluster.TopQuery,
                cluster.ClusterKey,
                cluster.TotalImpressions,
                cluster.AveragePosition,
                cluster.Queries.Take(5).ToArray()));
        }

        return new GscOwnerOverlay(
            Connected: true,
            Skipped: false,
            SkipReason: null,
            QueryRowCount: rows.Count,
            Clusters: clusters,
            Matches: matches);
    }

    internal static IReadOnlyList<TopicCandidate> ApplyToPool(
        IReadOnlyList<TopicCandidate> pool,
        GscOwnerOverlay overlay)
    {
        if (overlay.Skipped || overlay.Matches.Count == 0 || pool.Count == 0)
            return pool;

        var bySlug = pool.ToDictionary(c => c.Slug, c => c, StringComparer.OrdinalIgnoreCase);
        foreach (var match in overlay.Matches)
        {
            var pillar = FindBestPillarMatch(bySlug.Values, match.TopQuery);
            if (pillar is null)
                continue;

            if (!bySlug.TryGetValue(pillar.Slug, out var candidate))
                continue;

            var snippet = $"{match.TotalImpressions:N0} impressions · avg pos {match.AveragePosition:F1}";
            var evidence = candidate.Evidence.ToList();
            if (evidence.Any(e => e.Source.Equals("gsc", StringComparison.OrdinalIgnoreCase)))
                continue;

            evidence.Add(new TopicEvidence
            {
                Source = "gsc",
                Snippet = snippet,
                Url = match.TopQuery,
                Weight = TopicEvidenceWeights.Gsc,
            });

            var confidence = Math.Min(
                TopicEvidenceWeights.MaxConfidence,
                candidate.Confidence + TopicEvidenceWeights.Gsc);

            bySlug[pillar.Slug] = candidate with
            {
                Evidence = evidence,
                Confidence = confidence,
            };
        }

        return bySlug.Values.ToList();
    }

    internal static IReadOnlyList<string> FindSilentPillarSlugs(
        IReadOnlyList<DiscoveredPillar> selectedPillars,
        GscOwnerOverlay overlay)
    {
        if (!overlay.Connected || overlay.Skipped || overlay.Matches.Count == 0)
            return [];

        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var match in overlay.Matches)
        {
            var pillar = FindBestPillarMatch(
                selectedPillars.Select(p => new TopicCandidate
                {
                    Name = p.Name,
                    Slug = p.Slug,
                    Evidence = [],
                    Confidence = 0,
                }),
                match.TopQuery);
            if (pillar is not null)
                matched.Add(pillar.Slug);
        }

        return selectedPillars
            .Where(p => !matched.Contains(p.Slug))
            .Select(p => p.Slug)
            .ToList();
    }

    internal static TopicCandidate? FindBestPillarMatch(
        IEnumerable<TopicCandidate> pillars,
        string clusterTopQuery)
    {
        TopicCandidate? best = null;
        var bestScore = 0;

        foreach (var pillar in pillars)
        {
            var score = ScorePillarMatch(pillar.Name, pillar.Slug, clusterTopQuery);
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = pillar;
        }

        return bestScore >= 2 ? best : null;
    }

    internal static int ScorePillarMatch(string pillarName, string pillarSlug, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var normalizedQuery = query.ToLowerInvariant();
        var normalizedName = pillarName.ToLowerInvariant();
        var slugPhrase = pillarSlug.Replace('-', ' ');

        if (normalizedQuery.Contains(normalizedName, StringComparison.Ordinal))
            return 10;

        if (!string.IsNullOrWhiteSpace(slugPhrase)
            && normalizedQuery.Contains(slugPhrase, StringComparison.Ordinal))
            return 8;

        var score = 0;
        foreach (var word in normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length < 4)
                continue;

            if (normalizedQuery.Contains(word, StringComparison.Ordinal))
                score += 2;
        }

        return score;
    }
}

public sealed record GscPillarMatch(
    string TopQuery,
    string ClusterKey,
    long TotalImpressions,
    double AveragePosition,
    IReadOnlyList<string> SampleQueries);

public sealed record GscOwnerOverlay(
    bool Connected,
    bool Skipped,
    string? SkipReason,
    int QueryRowCount,
    IReadOnlyList<TopicClusteringService.QueryClusterDraft> Clusters,
    IReadOnlyList<GscPillarMatch> Matches)
{
    internal static GscOwnerOverlay Unavailable(bool connected, string reason) =>
        new(connected, true, reason, 0, [], []);
}
