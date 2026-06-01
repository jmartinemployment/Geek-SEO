using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

public sealed class ContentGuardService(
    IContentGuardRepository guard,
    PublishedContentAuditService auditService,
    IProjectRepository projects,
    IWordPressConnectionRepository connections,
    IWordPressProvider wordpress,
    IAIProvider ai,
    IHttpClientFactory httpClientFactory)
{
    /// <summary>Cap AI auto-patches per HTTP scan so Railway request timeout is not exceeded.</summary>
    private const int MaxAutoPatchesPerRequest = 5;
    public async Task<ContentGuardPolicyDto?> GetPolicyAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);
        var result = await guard.GetPolicyAsync(projectId, ct);
        if (!result.IsSuccess || result.Value is null)
            return null;

        return new ContentGuardPolicyDto
        {
            ProjectId = projectId,
            Enabled = result.Value.Enabled,
            AutoPatch = result.Value.AutoPatch,
        };
    }

    public async Task<ContentGuardPolicyDto> UpsertPolicyAsync(
        Guid userId,
        Guid projectId,
        UpsertContentGuardPolicyRequest request,
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);
        var saved = await guard.UpsertPolicyAsync(new SeoContentGuardPolicy
        {
            ProjectId = projectId,
            UserId = userId,
            Enabled = request.Enabled,
            AutoPatch = request.AutoPatch,
        }, ct);

        if (!saved.IsSuccess || saved.Value is null)
            throw new InvalidOperationException(saved.Error ?? "Failed to save policy");

        return new ContentGuardPolicyDto
        {
            ProjectId = projectId,
            Enabled = saved.Value.Enabled,
            AutoPatch = saved.Value.AutoPatch,
        };
    }

    public async Task<IReadOnlyList<ContentGuardRunDto>> ListRunsAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);
        var result = await guard.ListRunsAsync(projectId, 50, ct);
        if (!result.IsSuccess || result.Value is null)
            return [];

        return result.Value.Select(MapRun).ToList();
    }

    public async Task<ContentGuardScanSummary> ScanProjectAsync(
        Guid userId,
        Guid projectId,
        bool autoPatch,
        CancellationToken ct = default)
    {
        var report = await auditService.AnalyzeAsync(userId, projectId, ct);
        var decaying = report.Pages.Where(p => p.Status is "decaying" or "critical").ToList();
        var summary = new ContentGuardScanSummary
        {
            DecayingPagesFound = decaying.Count,
        };

        var runsCreated = 0;
        var patchesAttempted = 0;
        var patchesSucceeded = 0;
        var patchesFailed = 0;

        foreach (var page in decaying)
        {
            ct.ThrowIfCancellationRequested();

            var run = new SeoContentGuardRun
            {
                ProjectId = projectId,
                UserId = userId,
                Url = page.Url,
                Status = "detected",
                Recommendation = page.Recommendation,
                DetectedAt = DateTimeOffset.UtcNow,
            };

            var created = await guard.CreateRunAsync(run, ct);
            if (!created.IsSuccess || created.Value is null)
                continue;

            runsCreated++;
            if (!autoPatch)
                continue;

            if (patchesAttempted >= MaxAutoPatchesPerRequest)
                continue;

            patchesAttempted++;
            var patchOutcome = await TryPatchRunAsync(userId, created.Value, ct);
            if (patchOutcome)
                patchesSucceeded++;
            else
                patchesFailed++;
        }

        return summary with
        {
            RunsCreated = runsCreated,
            PatchesAttempted = patchesAttempted,
            PatchesSucceeded = patchesSucceeded,
            PatchesFailed = patchesFailed,
        };
    }

    public async Task<ContentGuardRunDto> ApproveRunAsync(Guid userId, Guid runId, CancellationToken ct = default)
    {
        var runResult = await guard.GetRunAsync(runId, ct);
        if (!runResult.IsSuccess || runResult.Value is null)
            throw new InvalidOperationException("Run not found");

        await EnsureProjectAsync(userId, runResult.Value.ProjectId, ct);
        var run = runResult.Value;
        run.Status = "approved";
        run.CompletedAt = DateTimeOffset.UtcNow;
        var updated = await guard.UpdateRunAsync(run, ct);
        if (!updated.IsSuccess || updated.Value is null)
            throw new InvalidOperationException(updated.Error ?? "Failed to approve run");

        return MapRun(updated.Value);
    }

    public async Task<ContentGuardRunDto> RollbackRunAsync(Guid userId, Guid runId, CancellationToken ct = default)
    {
        var runResult = await guard.GetRunAsync(runId, ct);
        if (!runResult.IsSuccess || runResult.Value is null)
            throw new InvalidOperationException("Run not found");

        var run = runResult.Value;
        await EnsureProjectAsync(userId, run.ProjectId, ct);

        if (string.IsNullOrWhiteSpace(run.PrePatchHtml) || run.DocumentId is null)
            throw new InvalidOperationException("No pre-patch snapshot stored for rollback");

        run.Status = "rolled_back";
        run.CompletedAt = DateTimeOffset.UtcNow;
        var updated = await guard.UpdateRunAsync(run, ct);
        if (!updated.IsSuccess || updated.Value is null)
            throw new InvalidOperationException(updated.Error ?? "Failed to rollback run");

        return MapRun(updated.Value);
    }

    /// <returns>True when patch completed (draft_ready or detected without WP); false when failed.</returns>
    private async Task<bool> TryPatchRunAsync(Guid userId, SeoContentGuardRun run, CancellationToken ct)
    {
        try
        {
            var http = httpClientFactory.CreateClient("PublicScanPage");
            string liveHtml;
            try
            {
                liveHtml = await http.GetStringAsync(run.Url, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                run.Status = "failed";
                run.Recommendation = "Could not fetch live page for patching.";
                run.CompletedAt = DateTimeOffset.UtcNow;
                await guard.UpdateRunAsync(run, ct);
                return false;
            }

            run.PrePatchHtml = liveHtml;
            run.Status = "patching";
            await guard.UpdateRunAsync(run, ct);

            var aiResult = await ai.CompleteAsync(new AIRequest
            {
                SystemPrompt =
                    "You refresh decaying SEO content. Preserve factual claims, improve clarity, update stale phrasing, and return improved HTML body only (h1/h2/p/ul). No markdown fences.",
                UserPrompt = $"URL: {run.Url}\nRecommendation: {run.Recommendation}\n\nHTML:\n{liveHtml}",
                MaxTokens = 8192,
                Temperature = 0.4,
            }, ct);

            if (!aiResult.IsSuccess || aiResult.Value is null)
            {
                run.Status = "failed";
                run.Recommendation = aiResult.Error ?? "AI refresh failed.";
                run.CompletedAt = DateTimeOffset.UtcNow;
                await guard.UpdateRunAsync(run, ct);
                return false;
            }

            run.PatchedHtml = AiHtmlSanitizer.ToHtmlFragment(aiResult.Value.Content);

            var conn = await connections.GetByProjectAsync(run.ProjectId, ct);
            if (conn.IsSuccess && conn.Value is not null)
            {
                try
                {
                    var password = SeoCredentialProtector.Decrypt(
                        conn.Value.EncryptedAppPassword, conn.Value.EncryptionIv, conn.Value.EncryptionTag);
                    var credentials = new WordPressCredentials
                    {
                        SiteUrl = conn.Value.SiteUrl,
                        Username = conn.Value.Username,
                        ApplicationPassword = password,
                    };

                    var draft = await wordpress.PublishPostAsync(credentials, new WordPressPostPayload
                    {
                        Title = $"[Content Guard] Refresh — {run.Url}",
                        ContentHtml = run.PatchedHtml,
                        Status = "draft",
                    }, ct);

                    if (draft.IsSuccess && draft.Value is not null)
                        run.WordPressDraftPostId = draft.Value.PostId;
                    else if (!string.IsNullOrWhiteSpace(draft.Error))
                        run.Recommendation = $"{run.Recommendation} WordPress draft: {draft.Error}";
                }
                catch (InvalidOperationException ex)
                {
                    run.Recommendation =
                        $"{run.Recommendation} WordPress draft skipped: {ex.Message}";
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    run.Recommendation =
                        $"{run.Recommendation} WordPress draft skipped: stored credentials could not be decrypted.";
                }
            }

            run.Status = "draft_ready";
            run.CompletedAt = DateTimeOffset.UtcNow;
            await guard.UpdateRunAsync(run, ct);
            return true;
        }
        catch (Exception)
        {
            run.Status = "failed";
            run.Recommendation = "Auto-patch failed unexpectedly. Try again or disable auto-patch.";
            run.CompletedAt = DateTimeOffset.UtcNow;
            await guard.UpdateRunAsync(run, ct);
            return false;
        }
    }

    private async Task EnsureProjectAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");
    }

    private static ContentGuardRunDto MapRun(SeoContentGuardRun run) => new()
    {
        Id = run.Id,
        ProjectId = run.ProjectId,
        DocumentId = run.DocumentId,
        Url = run.Url,
        Status = run.Status,
        Recommendation = run.Recommendation,
        WordPressDraftPostId = run.WordPressDraftPostId,
        DetectedAt = run.DetectedAt.ToString("O"),
        CompletedAt = run.CompletedAt?.ToString("O"),
    };
}
