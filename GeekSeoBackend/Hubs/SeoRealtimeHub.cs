using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Hubs;

/// <summary>Shared realtime channel for site analysis, URL research, and other SEO jobs.</summary>
[Authorize]
public sealed class SeoRealtimeHub : Hub
{
    public Task JoinSiteAnalysisProfile(Guid profileId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, SiteAnalysisGroup(profileId));

    public Task LeaveSiteAnalysisProfile(Guid profileId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, SiteAnalysisGroup(profileId));

    public static string SiteAnalysisGroup(Guid profileId) => $"site-{profileId}";
}
