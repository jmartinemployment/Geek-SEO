using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class ApplySourcesJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ApplySourcesJobChannel channel,
    ILogger<ApplySourcesJobWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — ApplySourcesJobWorker idle");
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
                logger.LogError(ex, "ApplySourcesJobWorker iteration failed");
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
            var pending = await jobs.GetPendingAsync(IApplySourcesJobService.JobType, 50, ct);
            if (pending.IsSuccess && pending.Value is { Count: > 0 })
            {
                foreach (var _ in pending.Value)
                    channel.Notify();
                logger.LogInformation(
                    "ApplySourcesJobWorker: {Count} queued job(s) found on startup",
                    pending.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ApplySourcesJobWorker startup drain failed");
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
        var richText = scope.ServiceProvider.GetRequiredService<IRichTextProvider>();
        var sourceDiscovery = scope.ServiceProvider.GetRequiredService<ISourceDiscoveryService>();
        var notifier = scope.ServiceProvider.GetRequiredService<ContentDraftProgressNotifier>();

        var pending = await jobs.GetPendingAsync(IApplySourcesJobService.JobType, 1, ct);
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

        var payload = JsonSerializer.Deserialize<ApplySourcesJobPayload>(job.PayloadJson, JsonOptions);
        if (payload is null)
        {
            await jobs.MarkFailedAsync(job.Id, "Invalid job payload", ct);
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

        var doc = access.Value;
        var documentExtras = new DraftJobProgressExtras { DocumentId = payload.DocumentId };

        await jobs.UpdateProgressAsync(job.Id, 20, ct);
        await DraftJobHubPush.PushProgressAsync(jobs, notifier, job.Id, job.UserId, documentExtras, ct);

        var excerpt = richText.ExtractPlainText(doc.ContentHtml);
        var discovered = await sourceDiscovery.DiscoverAsync(
            doc.ProjectId,
            payload.Keyword,
            payload.Location,
            excerpt,
            ct);

        if (!discovered.IsSuccess || discovered.Value is null)
        {
            await jobs.MarkFailedAsync(job.Id, discovered.Error ?? "Source discovery failed", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        await jobs.UpdateProgressAsync(job.Id, 70, ct);
        await DraftJobHubPush.PushProgressAsync(jobs, notifier, job.Id, job.UserId, documentExtras, ct);

        var patchedHtml = ScoreSuggestionApplicator.TryAppendSourcesFromDiscovered(doc.ContentHtml, discovered.Value);
        if (string.IsNullOrWhiteSpace(patchedHtml))
        {
            await jobs.MarkFailedAsync(
                job.Id,
                ScoreSuggestionApplicator.DescribeDeterministicFailure("geo_citations", doc.ContentHtml, payload.Keyword),
                ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        var updated = await documents.UpdateContentAsync(job.UserId, payload.DocumentId, new UpdateContentRequest
        {
            ContentHtml = patchedHtml,
        }, ct);

        if (!updated.IsSuccess)
        {
            await jobs.MarkFailedAsync(job.Id, updated.Error ?? "Failed to save sources", ct);
            await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
            return;
        }

        await jobs.MarkCompleteAsync(job.Id, payload.DocumentId, ct);
        await DraftJobHubPush.PushTerminalAsync(jobs, notifier, job.Id, job.UserId, ct);
        logger.LogInformation(
            "Apply sources job {JobId} completed → document {DocumentId}",
            job.Id,
            payload.DocumentId);
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
