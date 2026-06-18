using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

internal static class DraftJobHubPush
{
    internal static BackgroundJobStatus ToStatus(SeoBackgroundJob job) => new()
    {
        JobId = job.Id,
        JobType = job.JobType,
        Status = job.Status,
        ProgressPercent = job.ProgressPercent,
        ResultId = job.ResultId,
        ErrorMessage = job.ErrorMessage,
    };

    internal static async Task PushProgressAsync(
        IBackgroundJobRepository jobs,
        ContentDraftProgressNotifier notifier,
        Guid jobId,
        Guid userId,
        DraftJobProgressExtras? extras,
        CancellationToken ct)
    {
        var jobResult = await jobs.GetByIdAsync(jobId, ct);
        if (!jobResult.IsSuccess || jobResult.Value is null) return;
        await notifier.PushProgressAsync(userId, ToStatus(jobResult.Value), extras, ct);
    }

    internal static async Task PushTerminalAsync(
        IBackgroundJobRepository jobs,
        ContentDraftProgressNotifier notifier,
        Guid jobId,
        Guid userId,
        CancellationToken ct)
    {
        var jobResult = await jobs.GetByIdAsync(jobId, ct);
        if (!jobResult.IsSuccess || jobResult.Value is null) return;
        await notifier.PushCompleteAsync(userId, ToStatus(jobResult.Value), ct);
    }
}
