using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Pipeline;

public class BoundedPageRankService(AppDbContext db)
{
    private const double Damping = 0.85;
    private const int Iterations = 20;

    public async Task<int> RunPageRankStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var pages = await db.Pages.Where(p => p.RunId == runId).ToListAsync(ct);
        var pageIds = pages.Select(p => p.Id).ToHashSet();

        var internalEdges = await db.InternalLinks
            .Where(l => l.RunId == runId)
            .Select(l => new { l.FromPageId, l.ToPageId })
            .ToListAsync(ct);

        var crossEdges = await db.CrossRunLinks
            .Where(l => l.RunId == runId && !l.IsInternalToDomain)
            .Select(l => new { l.FromPageId, l.ToPageId })
            .ToListAsync(ct);

        var targetPageIds = pages.Where(p => p.IsTargetSite).Select(p => p.Id).ToHashSet();
        var serpPageIds = pages.Where(p => !p.IsTargetSite).Select(p => p.Id).ToHashSet();

        var targetInternalEdges = internalEdges
            .Where(e => targetPageIds.Contains(e.FromPageId) && targetPageIds.Contains(e.ToPageId))
            .Select(e => (e.FromPageId, e.ToPageId))
            .ToList();

        var serpSetEdges = crossEdges
            .Where(e => pageIds.Contains(e.FromPageId) && pageIds.Contains(e.ToPageId))
            .Select(e => (e.FromPageId, e.ToPageId))
            .ToList();

        var scores = new List<PageRankScore>();

        scores.AddRange(ComputeScopeScores(run, targetPageIds, targetInternalEdges, GraphScope.TargetInternal));
        scores.AddRange(ComputeScopeScores(run, serpPageIds, serpSetEdges, GraphScope.SerpSet));

        await db.PageRankScores.AddRangeAsync(scores, ct);
        await db.SaveChangesAsync(ct);

        return scores.Count;
    }

    private static IEnumerable<PageRankScore> ComputeScopeScores(
        AnalysisRun run,
        HashSet<Guid> nodeIds,
        IReadOnlyList<(Guid From, Guid To)> edges,
        GraphScope scope)
    {
        if (nodeIds.Count == 0)
            yield break;

        var nodes = nodeIds.ToList();
        var index = nodes.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var n = nodes.Count;
        var ranks = Enumerable.Repeat(1.0 / n, n).ToArray();
        var outLinks = nodes.Select(_ => new List<int>()).ToArray();
        var inLinks = nodes.Select(_ => new List<int>()).ToArray();

        foreach (var (from, to) in edges)
        {
            if (!index.TryGetValue(from, out var fromIdx) || !index.TryGetValue(to, out var toIdx))
                continue;

            outLinks[fromIdx].Add(toIdx);
            inLinks[toIdx].Add(fromIdx);
        }

        for (var iter = 0; iter < Iterations; iter++)
        {
            var next = new double[n];
            for (var i = 0; i < n; i++)
            {
                var sum = 0.0;
                foreach (var source in inLinks[i])
                {
                    var outCount = outLinks[source].Count;
                    sum += outCount == 0 ? 0 : ranks[source] / outCount;
                }

                next[i] = (1 - Damping) / n + Damping * sum;
            }

            ranks = next;
        }

        foreach (var (pageId, idx) in index)
        {
            yield return new PageRankScore
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                PageId = pageId,
                GraphScope = scope,
                Score = ranks[idx]
            };
        }
    }
}
