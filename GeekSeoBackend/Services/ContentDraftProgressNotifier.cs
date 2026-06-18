using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Services;

/// <summary>Pushes content draft and bulk job progress to the owning user (all tabs).</summary>
public sealed class ContentDraftProgressNotifier(
    IHubContext<SeoContentScoringHub> hub,
    ILogger<ContentDraftProgressNotifier> logger)
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task PushProgressAsync(
        Guid userId,
        BackgroundJobStatus status,
        DraftJobProgressExtras? extras = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            JobId = status.JobId,
            JobType = status.JobType,
            Status = status.Status,
            ProgressPercent = status.ProgressPercent,
            ResultId = status.ResultId,
            ErrorMessage = status.ErrorMessage,
            Keyword = extras?.Keyword,
            KeywordIndex = extras?.KeywordIndex,
            KeywordTotal = extras?.KeywordTotal,
            DocumentId = extras?.DocumentId,
        };

        await SendAsync(userId, SeoHubClientEvents.DraftJobProgress, payload, ct);
    }

    public async Task PushCompleteAsync(
        Guid userId,
        BackgroundJobStatus status,
        CancellationToken ct = default)
    {
        var payload = new
        {
            JobId = status.JobId,
            JobType = status.JobType,
            Status = status.Status,
            ProgressPercent = status.ProgressPercent,
            ResultId = status.ResultId,
            ErrorMessage = status.ErrorMessage,
        };

        await SendAsync(userId, SeoHubClientEvents.DraftJobComplete, payload, ct);
    }

    private async Task SendAsync(Guid userId, string method, object payload, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await hub.Clients.User(userId.ToString()).SendAsync(method, payload, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Draft job SignalR push attempt {Attempt}/{MaxAttempts} failed for user {UserId}",
                    attempt,
                    MaxAttempts,
                    userId);
                await Task.Delay(RetryDelay * attempt, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Draft job SignalR push failed after {MaxAttempts} attempts for user {UserId}",
                    MaxAttempts,
                    userId);
            }
        }
    }
}

public sealed record DraftJobProgressExtras
{
    public string? Keyword { get; init; }
    public int? KeywordIndex { get; init; }
    public int? KeywordTotal { get; init; }
    public Guid? DocumentId { get; init; }
}
