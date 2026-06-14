using GeekSeoBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Services;

/// <summary>
/// Pushes niche analysis progress over SignalR with retries and dual delivery (user + profile group).
/// </summary>
public sealed class NicheAnalysisProgressNotifier(
    IHubContext<SeoContentScoringHub> hub,
    ILogger<NicheAnalysisProgressNotifier> logger)
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task PushAsync(
        Guid profileId,
        Guid userId,
        string slug,
        string status,
        string message,
        int? stepNumber = null,
        int? totalSteps = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            ProfileId = profileId,
            Step = slug,
            Status = status,
            Message = message,
            StepNumber = stepNumber,
            TotalSteps = totalSteps,
        };

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var userTarget = hub.Clients.User(userId.ToString());
                var groupTarget = hub.Clients.Group(NicheProfileGroup(profileId));
                await userTarget.SendAsync("AnalysisProgress", payload, ct);
                await groupTarget.SendAsync("AnalysisProgress", payload, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "SignalR push attempt {Attempt}/{MaxAttempts} failed for profile {ProfileId} step {Slug}",
                    attempt,
                    MaxAttempts,
                    profileId,
                    slug);
                await Task.Delay(RetryDelay * attempt, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "SignalR push failed after {MaxAttempts} attempts for profile {ProfileId} step {Slug}",
                    MaxAttempts,
                    profileId,
                    slug);
            }
        }
    }

    public static string NicheProfileGroup(Guid profileId) => $"niche-{profileId}";
}
