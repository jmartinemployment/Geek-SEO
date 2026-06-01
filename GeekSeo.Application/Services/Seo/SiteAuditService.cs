using System.Text.Json;
using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeekSeo.Application.Services.Seo;

public sealed class SiteAuditService(
    IProjectRepository projects,
    ISiteAuditRepository audits,
    ICrawlerProvider crawler,
    ISubscriptionService subscription,
    IUsageMeteringService metering,
    IServiceScopeFactory scopeFactory,
    IBackgroundUserContext backgroundUser,
    ILogger<SiteAuditService> logger) : ISiteAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SiteAuditSummaryDto>> StartAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        if (string.Equals(crawler.ProviderName, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            return Result<SiteAuditSummaryDto>.Failure(
                "Site crawl is unavailable on this server (Playwright disabled). Contact support or retry after deploy.");
        }

        var projectResult = await projects.GetByIdAsync(projectId, userId, ct);
        if (!projectResult.IsSuccess || projectResult.Value is null)
        {
            return projectResult.Status == ResultStatus.NotFound
                ? Result<SiteAuditSummaryDto>.NotFound(projectResult.Error ?? "Project not found")
                : Result<SiteAuditSummaryDto>.Failure(projectResult.Error ?? "Project lookup failed");
        }

        var tierResult = await subscription.GetActiveTierAsync(userId, ct);
        if (!tierResult.IsSuccess)
            return Result<SiteAuditSummaryDto>.Failure(tierResult.Error ?? "Subscription lookup failed");

        var withinLimit = await metering.EnsureWithinLimitAsync(userId, tierResult.Value, "site_audit", ct);
        if (!withinLimit.IsSuccess)
            return Result<SiteAuditSummaryDto>.Failure(withinLimit.Error ?? "Usage limit reached");

        var audit = new SeoSiteAudit
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow,
        };

        var created = await audits.CreateAsync(audit, ct);
        if (!created.IsSuccess || created.Value is null)
            return Result<SiteAuditSummaryDto>.Failure(created.Error ?? "Could not create site audit");

        var auditId = created.Value.Id;
        var siteUrl = projectResult.Value.Url;

        _ = Task.Run(
            () => ExecuteAuditAsync(userId, auditId, projectId, siteUrl, CancellationToken.None),
            CancellationToken.None);

        var incremented = await metering.IncrementAsync(userId, "site_audit", 1, ct);
        if (!incremented.IsSuccess)
            logger.LogWarning("Site audit {AuditId} started but usage increment failed: {Error}", auditId, incremented.Error);

        return Result<SiteAuditSummaryDto>.Success(ToSummary(created.Value));
    }

    public async Task<Result<SiteAuditDetailDto>> GetAsync(Guid userId, Guid auditId, CancellationToken ct = default)
    {
        var auditResult = await audits.GetByIdAsync(auditId, ct);
        if (!auditResult.IsSuccess || auditResult.Value is null)
        {
            return auditResult.Status == ResultStatus.NotFound
                ? Result<SiteAuditDetailDto>.NotFound(auditResult.Error ?? "Site audit not found")
                : Result<SiteAuditDetailDto>.Failure(auditResult.Error ?? "Site audit lookup failed");
        }

        var owned = await projects.GetByIdAsync(auditResult.Value.ProjectId, userId, ct);
        if (!owned.IsSuccess)
        {
            return owned.Status == ResultStatus.NotFound
                ? Result<SiteAuditDetailDto>.NotFound("Project not found")
                : Result<SiteAuditDetailDto>.Failure(owned.Error ?? "Project lookup failed");
        }

        return Result<SiteAuditDetailDto>.Success(ToDetail(auditResult.Value));
    }

    public async Task<Result<IReadOnlyList<SiteAuditSummaryDto>>> ListByProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        var owned = await projects.GetByIdAsync(projectId, userId, ct);
        if (!owned.IsSuccess)
        {
            return owned.Status == ResultStatus.NotFound
                ? Result<IReadOnlyList<SiteAuditSummaryDto>>.NotFound(owned.Error ?? "Project not found")
                : Result<IReadOnlyList<SiteAuditSummaryDto>>.Failure(owned.Error ?? "Project lookup failed");
        }

        var list = await audits.ListByProjectAsync(projectId, ct);
        if (!list.IsSuccess)
            return Result<IReadOnlyList<SiteAuditSummaryDto>>.Failure(list.Error ?? "List failed");

        var summaries = (list.Value ?? []).Select(ToSummary).ToList();
        return Result<IReadOnlyList<SiteAuditSummaryDto>>.Success(summaries);
    }

    private async Task ExecuteAuditAsync(
        Guid userId,
        Guid auditId,
        Guid projectId,
        string siteUrl,
        CancellationToken ct)
    {
        backgroundUser.SetUserId(userId);

        using var scope = scopeFactory.CreateScope();
        var scopedAudits = scope.ServiceProvider.GetRequiredService<ISiteAuditRepository>();
        var scopedCrawler = scope.ServiceProvider.GetRequiredService<ICrawlerProvider>();

        try
        {
            var urls = await SiteAuditUrlDiscovery.DiscoverAsync(siteUrl, SiteAuditPageAnalyzer.MaxPagesPerRun, ct);
            var pageInputs = new List<SiteAuditPageInput>();
            var scores = new List<int>();

            foreach (var url in urls)
            {
                ct.ThrowIfCancellationRequested();
                var crawled = await scopedCrawler.CrawlPageAsync(url, ct);
                if (!crawled.IsSuccess || crawled.Value is null)
                    continue;

                var (score, issuesJson) = SiteAuditPageAnalyzer.Analyze(crawled.Value);
                scores.Add(score);
                pageInputs.Add(new SiteAuditPageInput(url, score, issuesJson, crawled.Value.CrawledAt));
            }

            if (pageInputs.Count > 0)
            {
                var appended = await scopedAudits.AppendPagesAsync(auditId, new AppendSiteAuditPagesRequest(pageInputs), ct);
                if (!appended.IsSuccess)
                    logger.LogWarning("Site audit {AuditId} append pages failed: {Error}", auditId, appended.Error);
            }

            var overall = scores.Count > 0 ? (decimal)Math.Round(scores.Average(), 1) : 0;
            var status = pageInputs.Count > 0 ? "completed" : "failed";
            var error = pageInputs.Count > 0 ? null : "No pages could be crawled. Check the site URL and robots.txt.";

            var updated = await scopedAudits.UpdateStatusAsync(
                auditId,
                new UpdateSiteAuditStatusRequest(
                    status,
                    pageInputs.Count,
                    overall,
                    error,
                    DateTimeOffset.UtcNow),
                ct);
            if (!updated.IsSuccess)
                logger.LogError("Site audit {AuditId} could not update status to {Status}: {Error}", auditId, status, updated.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Site audit {AuditId} failed for project {ProjectId}", auditId, projectId);
            var updated = await scopedAudits.UpdateStatusAsync(
                auditId,
                new UpdateSiteAuditStatusRequest(
                    "failed",
                    0,
                    null,
                    ex.Message,
                    DateTimeOffset.UtcNow),
                ct);
            if (!updated.IsSuccess)
                logger.LogError("Site audit {AuditId} could not mark failed: {Error}", auditId, updated.Error);
        }
    }

    private static SiteAuditSummaryDto ToSummary(SeoSiteAudit audit) =>
        new(
            audit.Id,
            audit.ProjectId,
            audit.Status,
            audit.PagesCrawled,
            audit.OverallScore,
            audit.ErrorMessage,
            audit.StartedAt,
            audit.CompletedAt);

    private static SiteAuditDetailDto ToDetail(SeoSiteAudit audit)
    {
        var pages = (audit.Pages ?? [])
            .OrderByDescending(p => p.CrawledAt)
            .Select(p => new SiteAuditPageDto(
                p.Id,
                p.Url,
                p.Score,
                ParseIssues(p.IssuesJson),
                p.CrawledAt))
            .ToList();

        return new SiteAuditDetailDto(
            audit.Id,
            audit.ProjectId,
            audit.Status,
            audit.PagesCrawled,
            audit.OverallScore,
            audit.ErrorMessage,
            audit.StartedAt,
            audit.CompletedAt,
            pages);
    }

    private static IReadOnlyList<SiteAuditIssue> ParseIssues(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<SiteAuditIssue>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
