using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class BulkArticleJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ILogger<BulkArticleJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BulkArticleJobWorker iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(6), stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobRepository>();
        var briefs = scope.ServiceProvider.GetRequiredService<IContentBriefService>();
        var writing = scope.ServiceProvider.GetRequiredService<IAIWritingService>();
        var documents = scope.ServiceProvider.GetRequiredService<IContentDocumentService>();

        var pending = await jobs.GetPendingAsync("bulk_article", 1, ct);
        if (!pending.IsSuccess || pending.Value is null || pending.Value.Count == 0)
            return;

        var job = pending.Value[0];
        workerUser.UserId = job.UserId;

        var payload = JsonSerializer.Deserialize<BulkArticleJobPayload>(job.PayloadJson, JsonOptions);
        if (payload is null || payload.Keywords.Count == 0)
        {
            await jobs.MarkFailedAsync(job.Id, "Invalid bulk job payload", ct);
            workerUser.UserId = Guid.Empty;
            return;
        }

        Guid? lastDocId = null;
        var total = payload.Keywords.Count;

        for (var i = payload.CurrentIndex; i < total; i++)
        {
            var keyword = payload.Keywords[i];
            var progress = (int)Math.Round((i / (double)total) * 90) + 5;
            await jobs.UpdateProgressAsync(job.Id, progress, ct);

            var title = keyword;
            var htmlResult = await ArticleGenerationPipeline.GenerateHtmlAsync(
                job.UserId,
                payload.ProjectId,
                keyword,
                payload.Location,
                title,
                briefs,
                writing,
                ct);

            if (!htmlResult.IsSuccess || htmlResult.Value is null)
            {
                await jobs.MarkFailedAsync(job.Id, $"Failed on \"{keyword}\": {htmlResult.Error}", ct);
                workerUser.UserId = Guid.Empty;
                return;
            }

            var create = await documents.CreateAsync(
                job.UserId,
                new CreateContentDocumentRequest
                {
                    ProjectId = payload.ProjectId,
                    Title = title,
                    TargetKeyword = keyword,
                    TargetLocation = payload.Location,
                },
                ct);

            if (!create.IsSuccess || create.Value is null)
            {
                await jobs.MarkFailedAsync(job.Id, create.Error ?? "Failed to create document", ct);
                workerUser.UserId = Guid.Empty;
                return;
            }

            lastDocId = create.Value.Id;
            var updated = await documents.UpdateContentAsync(
                job.UserId,
                lastDocId.Value,
                new UpdateContentRequest { ContentHtml = htmlResult.Value },
                ct);

            if (!updated.IsSuccess)
            {
                await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save content", ct);
                workerUser.UserId = Guid.Empty;
                return;
            }
        }

        await jobs.MarkCompleteAsync(job.Id, lastDocId, ct);
        logger.LogInformation("Bulk article job {JobId} completed {Count} articles", job.Id, total);
        workerUser.UserId = Guid.Empty;
    }
}
