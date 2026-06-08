using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class FullArticleJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    FullArticleJobChannel channel,
    ILogger<FullArticleJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — FullArticleJobWorker idle");
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
                logger.LogError(ex, "FullArticleJobWorker iteration failed");
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
            var pending = await jobs.GetPendingAsync("full_article", 50, ct);
            if (pending.IsSuccess && pending.Value is { Count: > 0 })
            {
                foreach (var _ in pending.Value)
                    channel.Notify();
                logger.LogInformation("FullArticleJobWorker: {Count} queued job(s) found on startup", pending.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FullArticleJobWorker startup drain failed");
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

        var pending = await jobs.GetPendingAsync("full_article", 1, ct);
        if (!pending.IsSuccess || pending.Value is null || pending.Value.Count == 0)
            return;

        var job = pending.Value[0];
        workerUser.UserId = job.UserId;

        await jobs.UpdateProgressAsync(job.Id, 5, ct);

        var payload = JsonSerializer.Deserialize<FullArticleJobPayload>(job.PayloadJson, JsonOptions);
        if (payload is null)
        {
            await jobs.MarkFailedAsync(job.Id, "Invalid job payload", ct);
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 15, ct);

        var htmlResult = await ArticleGenerationPipeline.GenerateHtmlAsync(
            job.UserId, payload.ProjectId, payload.Keyword, payload.Location, payload.Title,
            briefs, writing, ct);

        if (!htmlResult.IsSuccess || htmlResult.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, htmlResult.Error ?? "Article generation failed", ct);
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 80, ct);

        var create = await documents.CreateAsync(job.UserId, new CreateContentDocumentRequest
        {
            ProjectId = payload.ProjectId,
            Title = payload.Title,
            TargetKeyword = payload.Keyword,
            TargetLocation = payload.Location,
        }, ct);

        if (!create.IsSuccess || create.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, create.Error ?? "Failed to create document", ct);
            return;
        }

        var updated = await documents.UpdateContentAsync(job.UserId, create.Value.Id,
            new UpdateContentRequest { ContentHtml = htmlResult.Value }, ct);

        if (!updated.IsSuccess)
        {
            await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save content", ct);
            return;
        }

        await jobs.MarkCompleteAsync(job.Id, create.Value.Id, ct);
        logger.LogInformation("Full article job {JobId} completed → document {DocumentId}", job.Id, create.Value.Id);
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
