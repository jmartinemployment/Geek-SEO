using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services;

public interface IRunProgressNotifier
{
    Task NotifySerpClaimed(Guid runId, CancellationToken ct = default);

    Task NotifyStageCompleted(
        Guid runId,
        PipelineStage stage,
        bool passed,
        string message,
        RunStatus status,
        CancellationToken ct = default);

    Task NotifySiteProfileUpdated(
        Guid runId,
        Guid siteProfileId,
        CancellationToken ct = default);
}
