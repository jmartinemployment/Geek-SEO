using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Pipeline;

namespace SiteAnalyzer2.Services.SiteAudit;

/// <summary>
/// Orchestrates site audit crawl + checks. Persistence wired in slice 2a-1 (site_audit_runs migration).
/// </summary>
public sealed class SiteAuditJobService(
    IServiceScopeFactory scopeFactory,
    SiteAuditCheckService checkService,
    SiteAuditRollupService rollupService)
{
    public Task<SiteAuditJobState> GetLatestStateAsync(AppDbContext db, Guid siteProfileId, CancellationToken ct = default) =>
        throw new NotImplementedException("Slice 2a-1: query site_audit_runs after migration.");

    public Task<bool> TryStartAsync(AppDbContext db, Guid siteProfileId, CancellationToken ct = default) =>
        throw new NotImplementedException("Slice 2a-1: create site_audit_runs row and background ExecuteAsync.");

    /// <summary>
    /// Slice 2a-2/2a-3: crawl target site, run checks, persist findings, update rollup columns.
    /// </summary>
    public async Task<SiteAuditOverview> ExecuteAuditChecksAsync(
        SiteAuditRun auditRun,
        SiteAuditCheckInput input,
        CancellationToken ct = default)
    {
        var issues = checkService.RunAllChecks(input);
        return rollupService.BuildOverview(
            auditRun.Id,
            auditRun.SiteProfileId,
            SiteAuditStatuses.Complete,
            input.Pages,
            issues,
            DateTime.UtcNow,
            null);
    }

    private async Task ExecuteAsync(Guid auditRunId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = db;
            _ = scope.ServiceProvider.GetRequiredService<PageFetchService>();
            // 2a-2: bind crawl to audit run (not analysis_runs)
            // 2a-3: map pages → SiteAuditCheckInput, persist findings
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            var logger = scopeFactory.CreateScope().ServiceProvider.GetService<ILogger<SiteAuditJobService>>();
            logger?.LogWarning(ex, "Site audit failed for {AuditRunId}.", auditRunId);
        }
    }
}

public enum SiteAuditJobStatus
{
    Idle,
    Running,
    Complete,
    Failed,
}

public sealed class SiteAuditJobState
{
    public static SiteAuditJobState Idle { get; } = new() { Status = SiteAuditJobStatus.Idle };

    public SiteAuditJobStatus Status { get; init; } = SiteAuditJobStatus.Idle;
    public Guid? AuditRunId { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public string? Message { get; init; }
}
