using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Rankings;

public sealed record SerpRankSnapshotDto(
    int ImportSequence,
    DateTimeOffset SerpCapturedAt,
    int? TargetPosition,
    string? TargetUrl,
    int OrganicResultCount);

public sealed record RankingsDeltaDto(
    int? PreviousPosition,
    int? CurrentPosition,
    int? PositionChange,
    DateTimeOffset? PreviousCapturedAt,
    DateTimeOffset CurrentCapturedAt);

public sealed record RunRankingsSummaryDto(
    IReadOnlyList<SerpRankSnapshotDto> History,
    RankingsDeltaDto? LatestDelta,
    bool HasRecapture);

public sealed record SerpRankImportResult(
    int? TargetOrganicPosition,
    string? TargetOrganicUrl,
    RankingsDeltaDto? RankingsDelta);

public sealed class SerpRankHistoryService(AppDbContext db)
{
    public async Task<SerpRankImportResult?> RecordAfterImportAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return null;

        var items = await db.SerpItems.AsNoTracking()
            .Where(i => i.RunId == runId)
            .ToListAsync(ct);

        var organicCount = items.Count(SerpTargetRankResolver.IsOrganicCandidate);
        var rank = SerpTargetRankResolver.ResolveFromItems(run.TargetSiteUrl, items);

        var previousSequence = await db.SerpRankSnapshots.AsNoTracking()
            .Where(s => s.RunId == runId)
            .OrderByDescending(s => s.ImportSequence)
            .Select(s => (int?)s.ImportSequence)
            .FirstOrDefaultAsync(ct);

        var importSequence = (previousSequence ?? 0) + 1;
        var capturedAt = run.SerpCapturedAt ?? DateTime.UtcNow;

        var snapshot = new SerpRankSnapshot
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            ProjectId = run.ProjectId,
            ImportSequence = importSequence,
            SerpCapturedAt = DateTime.SpecifyKind(capturedAt, DateTimeKind.Utc),
            RecordedAt = DateTime.UtcNow,
            TargetOrganicPosition = rank.Position,
            TargetOrganicUrl = rank.Url,
            OrganicResultCount = organicCount,
        };

        db.SerpRankSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);

        if (importSequence < 2)
            return new SerpRankImportResult(rank.Position, rank.Url, null);

        var previous = await db.SerpRankSnapshots.AsNoTracking()
            .Where(s => s.RunId == runId && s.ImportSequence == importSequence - 1)
            .FirstOrDefaultAsync(ct);

        var delta = previous is null ? null : BuildDelta(previous, snapshot);
        return new SerpRankImportResult(rank.Position, rank.Url, delta);
    }

    public async Task<RunRankingsSummaryDto> GetSummaryAsync(Guid runId, CancellationToken ct = default)
    {
        var snapshots = await db.SerpRankSnapshots.AsNoTracking()
            .Where(s => s.RunId == runId)
            .OrderBy(s => s.ImportSequence)
            .ToListAsync(ct);

        if (snapshots.Count == 0)
            return new RunRankingsSummaryDto([], null, false);

        var history = snapshots
            .Select(s => new SerpRankSnapshotDto(
                s.ImportSequence,
                new DateTimeOffset(DateTime.SpecifyKind(s.SerpCapturedAt, DateTimeKind.Utc)),
                s.TargetOrganicPosition,
                s.TargetOrganicUrl,
                s.OrganicResultCount))
            .ToList();

        RankingsDeltaDto? delta = null;
        if (snapshots.Count >= 2)
        {
            var previous = snapshots[^2];
            var current = snapshots[^1];
            delta = BuildDelta(previous, current);
        }

        return new RunRankingsSummaryDto(history, delta, snapshots.Count >= 2);
    }

    internal static RankingsDeltaDto BuildDelta(SerpRankSnapshot previous, SerpRankSnapshot current)
    {
        var previousCaptured = new DateTimeOffset(DateTime.SpecifyKind(previous.SerpCapturedAt, DateTimeKind.Utc));
        var currentCaptured = new DateTimeOffset(DateTime.SpecifyKind(current.SerpCapturedAt, DateTimeKind.Utc));

        int? change = previous.TargetOrganicPosition is int prev
            && current.TargetOrganicPosition is int curr
            ? prev - curr
            : null;

        return new RankingsDeltaDto(
            previous.TargetOrganicPosition,
            current.TargetOrganicPosition,
            change,
            previousCaptured,
            currentCaptured);
    }
}
