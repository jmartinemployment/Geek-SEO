using System.Text.Json;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed class CompetitorCrawlProgressLogService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<string> AppendAndBuildPayloadAsync(
        CompetitorCrawlProgressEvent progress,
        CancellationToken ct = default)
    {
        var log = new CompetitorCrawlProgressLog
        {
            RunId = progress.RunId,
            CreatedAt = DateTime.UtcNow,
        };

        db.CompetitorCrawlProgressLogs.Add(log);
        await db.SaveChangesAsync(ct);

        var payload = JsonSerializer.Serialize(new
        {
            runId = progress.RunId,
            crawlStatus = progress.CrawlStatus,
            competitorSaved = progress.CompetitorSaved,
            totalPages = progress.TotalPages,
            domainCount = progress.DomainCount,
            message = progress.Message,
            qualityWarnings = progress.QualityWarnings,
            sequenceNumber = log.Id,
        }, JsonOptions);

        log.Payload = payload;
        await db.SaveChangesAsync(ct);

        return payload;
    }
}
