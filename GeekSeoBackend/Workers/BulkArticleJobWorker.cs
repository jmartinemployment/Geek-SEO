using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class BulkArticleJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    BulkArticleJobChannel channel,
    ILogger<BulkArticleJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — BulkArticleJobWorker idle");
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
                logger.LogError(ex, "BulkArticleJobWorker iteration failed");
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
            var pending = await jobs.GetPendingAsync("bulk_article", 50, ct);
            if (pending.IsSuccess && pending.Value is { Count: > 0 })
            {
                foreach (var _ in pending.Value)
                    channel.Notify();
                logger.LogInformation("BulkArticleJobWorker: {Count} queued job(s) found on startup", pending.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BulkArticleJobWorker startup drain failed");
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
        var briefs = scope.ServiceProvider.GetRequiredService<IContentBriefService>();
        var writing = scope.ServiceProvider.GetRequiredService<IAIWritingService>();
        var documents = scope.ServiceProvider.GetRequiredService<IContentDocumentService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ContentDraftProgressNotifier>();

        var pending = await jobs.GetPendingAsync("bulk_article", 1, ct);
        if (!pending.IsSuccess || pending.Value is null || pending.Value.Count == 0)
            return;

        var job = pending.Value[0];
        workerUser.UserId = job.UserId;

        var payload = JsonSerializer.Deserialize<BulkArticleJobPayload>(job.PayloadJson, JsonOptions);
        if (payload is null || payload.Keywords.Count == 0)
        {
            await jobs.MarkFailedAsync(job.Id, "Invalid bulk job payload", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        Guid? lastDocId = null;
        var total = payload.Keywords.Count;

        for (var i = payload.CurrentIndex; i < total; i++)
        {
            var keyword = payload.Keywords[i];
            var progress = (int)Math.Round((i / (double)total) * 90) + 5;
            await jobs.UpdateProgressAsync(job.Id, progress, ct);
            await DraftJobHubPush.PushProgressAsync(
                jobs,
                notifier,
                job.Id,
                job.UserId,
                new DraftJobProgressExtras
                {
                    Keyword = keyword,
                    KeywordIndex = i + 1,
                    KeywordTotal = total,
                },
                ct);

            var htmlResult = await ArticleGenerationPipeline.GenerateHtmlAsync(
                job.UserId, payload.ProjectId, keyword, payload.Location, keyword,
                briefs, writing, ct);

            if (!htmlResult.IsSuccess || htmlResult.Value is null)
            {
                await jobs.MarkFailedAsync(job.Id, $"Failed on \"{keyword}\": {htmlResult.Error}", ct);
                await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
                return;
            }

            var create = await documents.CreateAsync(job.UserId, new CreateContentDocumentRequest
            {
                ProjectId = payload.ProjectId,
                Title = keyword,
                TargetKeyword = keyword,
                TargetLocation = payload.Location,
            }, ct);

            if (!create.IsSuccess || create.Value is null)
            {
                await jobs.MarkFailedAsync(job.Id, create.Error ?? "Failed to create document", ct);
                await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
                return;
            }

            lastDocId = create.Value.Id;
            var updated = await documents.UpdateContentAsync(job.UserId, lastDocId.Value,
                new UpdateContentRequest { ContentHtml = htmlResult.Value }, ct);

            if (!updated.IsSuccess)
            {
                await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save content", ct);
                await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
                return;
            }
        }

        await jobs.MarkCompleteAsync(job.Id, lastDocId, ct);
        await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
        logger.LogInformation("Bulk article job {JobId} completed {Count} articles", job.Id, total);
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
