using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class FullArticleJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ILogger<FullArticleJobWorker> logger) : BackgroundService
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
                logger.LogError(ex, "FullArticleJobWorker iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
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
            workerUser.UserId = Guid.Empty;
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 15, ct);

        var htmlResult = await ArticleGenerationPipeline.GenerateHtmlAsync(
            job.UserId,
            payload.ProjectId,
            payload.Keyword,
            payload.Location,
            payload.Title,
            briefs,
            writing,
            ct);

        if (!htmlResult.IsSuccess || htmlResult.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, htmlResult.Error ?? "Article generation failed", ct);
            workerUser.UserId = Guid.Empty;
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 80, ct);

        var create = await documents.CreateAsync(
            job.UserId,
            new CreateContentDocumentRequest
            {
                ProjectId = payload.ProjectId,
                Title = payload.Title,
                TargetKeyword = payload.Keyword,
                TargetLocation = payload.Location,
            },
            ct);

        if (!create.IsSuccess || create.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, create.Error ?? "Failed to create document", ct);
            workerUser.UserId = Guid.Empty;
            return;
        }

        var docId = create.Value.Id;
        var updated = await documents.UpdateContentAsync(
            job.UserId,
            docId,
            new UpdateContentRequest { ContentHtml = htmlResult.Value },
            ct);

        if (!updated.IsSuccess)
        {
            await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save content", ct);
            workerUser.UserId = Guid.Empty;
            return;
        }

        await jobs.MarkCompleteAsync(job.Id, docId, ct);
        logger.LogInformation("Full article job {JobId} completed → document {DocumentId}", job.Id, docId);
        workerUser.UserId = Guid.Empty;
    }
}
