using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services;

public sealed class NoOpRunProgressNotifier : IRunProgressNotifier
{
    public Task NotifySerpClaimed(Guid runId, CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifyStageCompleted(
        Guid runId,
        PipelineStage stage,
        bool passed,
        string message,
        RunStatus status,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task NotifySiteProfileUpdated(Guid runId, Guid siteProfileId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
