using GeekSeoBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Services;

/// <summary>Pushes page URL research job status over SignalR (no client polling).</summary>
public sealed class UrlResearchProgressNotifier(
    IHubContext<SeoRealtimeHub> hub,
    ILogger<UrlResearchProgressNotifier> logger) : IUrlResearchProgressNotifier
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task PushAsync(
        Guid urlResearchId,
        Guid projectId,
        Guid userId,
        string status,
        string? message = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            UrlResearchId = urlResearchId,
            ProjectId = projectId,
            Status = status,
            Message = message,
            ErrorMessage = errorMessage,
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var userTarget = hub.Clients.User(userId.ToString());
                var researchTarget = hub.Clients.Group(ResearchGroup(urlResearchId));
                var projectTarget = hub.Clients.Group(ProjectGroup(projectId));
                await userTarget.SendAsync("UrlResearchProgress", payload, ct);
                await researchTarget.SendAsync("UrlResearchProgress", payload, ct);
                await projectTarget.SendAsync("UrlResearchProgress", payload, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Url research SignalR push attempt {Attempt}/{MaxAttempts} failed for {Id}",
                    attempt,
                    MaxAttempts,
                    urlResearchId);
                await Task.Delay(RetryDelay * attempt, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Url research SignalR push failed after {MaxAttempts} attempts for {Id}",
                    MaxAttempts,
                    urlResearchId);
            }
        }
    }

    public static string ResearchGroup(Guid urlResearchId) => $"url-research-{urlResearchId}";

    public static string ProjectGroup(Guid projectId) => $"url-research-project-{projectId}";
}
