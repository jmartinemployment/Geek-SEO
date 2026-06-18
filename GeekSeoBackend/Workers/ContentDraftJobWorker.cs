using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class ContentDraftJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ContentDraftJobChannel channel,
    ILogger<ContentDraftJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — ContentDraftJobWorker idle");
            return;
        }

        await DrainExistingAsync(serviceUserId, stoppingToken);

        await foreach (var _ in channel.Reader.ReadAllAsync(stoppingToken))
        {
            workerUser.UserId = serviceUserId;
            try
            {
                await ProcessNextAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ContentDraftJobWorker iteration failed");
            }
            finally
            {
                workerUser.UserId = Guid.Empty;
            }
        }
    }

    private async Task DrainExistingAsync(Guid serviceUserId, CancellationToken ct)
    {
        try
        {
            workerUser.UserId = serviceUserId;
            using var scope = services.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
            var pending = await jobs.GetPendingAsync(ContentDraftJobService.JobType, 50, ct);
            if (pending.IsSuccess && pending.Value is { Count: > 0 })
            {
                foreach (var _ in pending.Value)
                    channel.Notify();
                logger.LogInformation(
                    "ContentDraftJobWorker: {Count} queued job(s) found on startup",
                    pending.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ContentDraftJobWorker startup drain failed");
        }
        finally
        {
            workerUser.UserId = Guid.Empty;
        }
    }

    private async Task ProcessNextAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
        var documents = scope.ServiceProvider.GetRequiredService<IContentDocumentService>();
        var briefs = scope.ServiceProvider.GetRequiredService<IContentBriefService>();
        var writing = scope.ServiceProvider.GetRequiredService<IAIWritingService>();
        var researchWriting = scope.ServiceProvider.GetRequiredService<IContentResearchWritingService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ContentDraftProgressNotifier>();

        var pending = await jobs.GetPendingAsync(ContentDraftJobService.JobType, 1, ct);
        if (!pending.IsSuccess || pending.Value is null || pending.Value.Count == 0)
            return;

        var job = pending.Value[0];
        workerUser.UserId = job.UserId;

        await jobs.UpdateProgressAsync(job.Id, 5, ct);
        await DraftJobHubPush.PushProgressAsync(
            jobs,
            notifier,
            job.Id,
            job.UserId,
            new DraftJobProgressExtras { DocumentId = null },
            ct);

        var payload = JsonSerializer.Deserialize<ContentDraftJobPayload>(job.PayloadJson, JsonOptions);
        if (payload is null)
        {
            await jobs.MarkFailedAsync(job.Id, "Invalid job payload", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        var documentExtras = new DraftJobProgressExtras { DocumentId = payload.DocumentId };

        if (payload.Mode == "research")
        {
            await jobs.UpdateProgressAsync(job.Id, 20, ct);
            await DraftJobHubPush.PushProgressAsync(jobs, notifier, job.Id, job.UserId, documentExtras, ct);

            var draft = await researchWriting.DraftFromResearchAsync(job.UserId, payload.DocumentId, ct);
            if (!draft.IsSuccess)
            {
                await jobs.MarkFailedAsync(job.Id, draft.Error ?? "Research draft failed", ct);
                await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
                return;
            }

            await jobs.MarkCompleteAsync(job.Id, payload.DocumentId, ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            logger.LogInformation(
                "Content research draft job {JobId} completed → document {DocumentId}",
                job.Id,
                payload.DocumentId);
            return;
        }

        if (payload.Mode != "keyword")
        {
            await jobs.MarkFailedAsync(job.Id, $"Unknown draft mode: {payload.Mode}", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(payload.Keyword))
        {
            await jobs.MarkFailedAsync(job.Id, "Keyword is required for keyword draft jobs", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        var access = await documents.EnsureAccessAsync(job.UserId, payload.DocumentId, ct);
        if (!access.IsSuccess || access.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, access.Error ?? "Access denied", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        var title = string.IsNullOrWhiteSpace(payload.Title) ? payload.Keyword : payload.Title;
        var location = string.IsNullOrWhiteSpace(payload.Location) ? "United States" : payload.Location;
        var keywordExtras = new DraftJobProgressExtras
        {
            DocumentId = payload.DocumentId,
            Keyword = payload.Keyword,
        };

        await jobs.UpdateProgressAsync(job.Id, 15, ct);
        await DraftJobHubPush.PushProgressAsync(jobs, notifier, job.Id, job.UserId, keywordExtras, ct);

        var htmlResult = await ArticleGenerationPipeline.GenerateHtmlAsync(
            job.UserId,
            access.Value.ProjectId,
            payload.Keyword,
            location,
            title,
            briefs,
            writing,
            ct);

        if (!htmlResult.IsSuccess || htmlResult.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, htmlResult.Error ?? "Draft generation failed", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 85, ct);
        await DraftJobHubPush.PushProgressAsync(jobs, notifier, job.Id, job.UserId, keywordExtras, ct);

        var updated = await documents.UpdateContentAsync(job.UserId, payload.DocumentId, new UpdateContentRequest
        {
            ContentHtml = htmlResult.Value,
            Title = title,
            TargetKeyword = payload.Keyword,
            TargetLocation = location,
        }, ct);

        if (!updated.IsSuccess)
        {
            await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save draft", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        await jobs.MarkCompleteAsync(job.Id, payload.DocumentId, ct);
        await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
        logger.LogInformation(
            "Content keyword draft job {JobId} completed → document {DocumentId}",
            job.Id,
            payload.DocumentId);
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
