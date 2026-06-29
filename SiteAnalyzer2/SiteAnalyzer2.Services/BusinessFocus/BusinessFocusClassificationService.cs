using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Integrations;
using System.Text.Json;

namespace SiteAnalyzer2.Services.BusinessFocus;

public record ManualBusinessProfileInput(
    string BusinessType,
    IReadOnlyList<string> PrimaryServices,
    string? ServiceArea,
    string Description,
    string GeneratedSchemaJson,
    bool HasExistingSchema,
    bool? ExistingSchemaMatches);

public class BusinessFocusClassificationService(AppDbContext db, IBusinessFocusClassifier classifier)
{
    public async Task RunAfterExtractAsync(Guid runId, CancellationToken ct = default)
    {
        if (await db.TargetSiteBusinessProfiles.AnyAsync(p => p.RunId == runId, ct))
            return;

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var normalizedTargetUrl = NormalizeTargetUrl(run.TargetSiteUrl);
        var forceRefresh = string.Equals(
            Environment.GetEnvironmentVariable("BUSINESS_PROFILE_FORCE_REFRESH"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!forceRefresh)
        {
            var cached = await db.TargetSiteBusinessProfiles
                .Where(p => p.ProjectId == run.ProjectId
                    && p.TargetSiteUrl == normalizedTargetUrl
                    && p.RunId != runId)
                .OrderByDescending(p => p.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            if (cached is not null)
            {
                db.TargetSiteBusinessProfiles.Add(CopyProfile(cached, run, normalizedTargetUrl));
                await db.SaveChangesAsync(ct);
                return;
            }
        }

        var provider = BusinessFocusProviderConfiguration.ResolveEffectiveProvider();
        if (!BusinessFocusProviderConfiguration.UsesAutomaticClassification(provider))
            return;

        var input = await BuildInputAsync(run, normalizedTargetUrl, ct);
        var result = await classifier.ClassifyAsync(input, ct);

        db.TargetSiteBusinessProfiles.Add(new TargetSiteBusinessProfile
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            TargetSiteUrl = normalizedTargetUrl,
            BusinessType = result.BusinessType,
            PrimaryServicesJson = JsonSerializer.Serialize(result.PrimaryServices),
            ServiceArea = result.ServiceArea,
            Description = result.Description,
            GeneratedSchemaJson = result.GeneratedSchemaJson,
            HasExistingSchema = result.HasExistingSchema,
            ExistingSchemaMatches = result.ExistingSchemaMatches,
            GeneratedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertManualProfileAsync(Guid runId, ManualBusinessProfileInput input, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var extractGate = await db.RunGates
            .FirstOrDefaultAsync(g => g.RunId == runId && g.Stage == PipelineStage.Extract, ct);

        if (extractGate is not { Passed: true })
        {
            throw new InvalidOperationException(
                "Business profile cannot be saved until the Extract stage has passed.");
        }

        var normalizedTargetUrl = NormalizeTargetUrl(run.TargetSiteUrl);
        var profile = await db.TargetSiteBusinessProfiles.FirstOrDefaultAsync(p => p.RunId == runId, ct);

        if (profile is null)
        {
            profile = new TargetSiteBusinessProfile
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                TargetSiteUrl = normalizedTargetUrl
            };
            db.TargetSiteBusinessProfiles.Add(profile);
        }

        profile.BusinessType = input.BusinessType;
        profile.PrimaryServicesJson = JsonSerializer.Serialize(input.PrimaryServices);
        profile.ServiceArea = input.ServiceArea;
        profile.Description = input.Description;
        profile.GeneratedSchemaJson = input.GeneratedSchemaJson;
        profile.HasExistingSchema = input.HasExistingSchema;
        profile.ExistingSchemaMatches = input.ExistingSchemaMatches;
        profile.GeneratedAt = DateTime.UtcNow;
        profile.ReusedFromRunId = null;

        await db.SaveChangesAsync(ct);
    }

    private async Task<BusinessFocusInput> BuildInputAsync(
        AnalysisRun run,
        string normalizedTargetUrl,
        CancellationToken ct)
    {
        var targetPageIds = await db.Pages
            .Where(p => p.RunId == run.Id && p.IsTargetSite)
            .OrderBy(p => p.DepthFromHomepage)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var headings = await db.PageHeadings
            .Where(h => targetPageIds.Contains(h.PageId))
            .OrderBy(h => h.Sequence)
            .Select(h => $"H{h.Level}: {h.Text}")
            .ToListAsync(ct);

        var metaTags = await db.PageMetaTags
            .Where(m => targetPageIds.Contains(m.PageId))
            .Select(m => $"{m.NameOrProperty}: {m.Content}")
            .ToListAsync(ct);

        var jsonLdBlocks = await db.PageJsonLdBlocks
            .Where(j => targetPageIds.Contains(j.PageId))
            .Select(j => j.RawJson)
            .ToListAsync(ct);

        var contentBlocks = await db.PageContentBlocks
            .Where(b => targetPageIds.Contains(b.PageId) && b.Content != null && b.Content != "")
            .OrderBy(b => b.Sequence)
            .Select(b => b.Content!)
            .ToListAsync(ct);

        return new BusinessFocusInput(
            normalizedTargetUrl,
            headings,
            metaTags,
            jsonLdBlocks,
            contentBlocks,
            jsonLdBlocks.Count > 0);
    }

    private static TargetSiteBusinessProfile CopyProfile(
        TargetSiteBusinessProfile source,
        AnalysisRun run,
        string normalizedTargetUrl) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            TargetSiteUrl = normalizedTargetUrl,
            BusinessType = source.BusinessType,
            PrimaryServicesJson = source.PrimaryServicesJson,
            ServiceArea = source.ServiceArea,
            Description = source.Description,
            GeneratedSchemaJson = source.GeneratedSchemaJson,
            HasExistingSchema = source.HasExistingSchema,
            ExistingSchemaMatches = source.ExistingSchemaMatches,
            GeneratedAt = DateTime.UtcNow,
            ReusedFromRunId = source.ReusedFromRunId ?? source.RunId
        };

    public static string NormalizeTargetUrl(string targetSiteUrl) =>
        TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
}
