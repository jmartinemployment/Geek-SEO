using Microsoft.AspNetCore.SignalR;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Services;

namespace SiteAnalyzer2.Api.Hubs;

public class RunProgressHub : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task SubscribeToRun(string runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(runId));
    }

    public async Task UnsubscribeFromRun(string runId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(runId));
    }

    public static string GroupName(string runId) => $"run-{runId}";
}

public sealed class SignalRRunProgressNotifier(
    Microsoft.AspNetCore.SignalR.IHubContext<RunProgressHub> hubContext) : IRunProgressNotifier
{
    public Task NotifySerpClaimed(Guid runId, CancellationToken ct = default)
    {
        return hubContext.Clients.Group(RunProgressHub.GroupName(runId.ToString()))
            .SendAsync("SerpClaimed", new { runId }, ct);
    }

    public Task NotifyStageCompleted(
        Guid runId,
        PipelineStage stage,
        bool passed,
        string validationMessage,
        RunStatus status,
        CancellationToken ct = default)
    {
        return hubContext.Clients.Group(RunProgressHub.GroupName(runId.ToString()))
            .SendAsync("StageCompleted", new
            {
                runId,
                stage = stage.ToString(),
                passed,
                validationMessage,
                status = status.ToString()
            }, ct);
    }

    public Task NotifySiteProfileUpdated(Guid runId, Guid siteProfileId, CancellationToken ct = default)
    {
        return hubContext.Clients.Group(RunProgressHub.GroupName(runId.ToString()))
            .SendAsync("SiteProfileUpdated", new { runId, siteProfileId }, ct);
    }
}
