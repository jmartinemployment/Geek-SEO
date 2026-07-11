using System.Text.Json;
using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class PlagiarismService(
    IContentDocumentService documents,
    IPlagiarismRepository plagiarism,
    IPlagiarismProvider provider,
    ISubscriptionService subscription,
    IUsageMeteringService metering) : IPlagiarismService
{
    public const decimal PublishBlockThresholdPercent = 15m;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PlagiarismStatus GetStatus() => new(provider.IsConfigured, provider.ProviderName);

    public async Task<Result<PlagiarismCheckResult>> CheckDocumentAsync(
        Guid userId,
        PlagiarismCheckRequest request,
        CancellationToken ct = default)
    {
        if (!provider.IsConfigured)
        {
            return Result<PlagiarismCheckResult>.Failure(
                "Plagiarism checking is not configured. Set COPYSCAPE_USERNAME and COPYSCAPE_API_KEY on GeekSeoBackend.");
        }

        var access = await documents.EnsureAccessAsync(userId, request.DocumentId, ct);
        if (!access.IsSuccess || access.Value is null)
        {
            return access.Status == ResultStatus.NotFound
                ? Result<PlagiarismCheckResult>.NotFound(access.Error ?? "Document not found")
                : Result<PlagiarismCheckResult>.Failure(access.Error ?? "Document access denied");
        }

        if (!request.ForceRefresh)
        {
            var cached = await TryGetFreshCacheAsync(request.DocumentId, ct);
            if (cached is not null)
                return Result<PlagiarismCheckResult>.Success(cached with { Cached = true });
        }

        var tierResult = await subscription.GetActiveTierAsync(userId, ct);
        if (!tierResult.IsSuccess)
            return Result<PlagiarismCheckResult>.Failure(tierResult.Error ?? "Subscription lookup failed");

        var withinLimit = await metering.EnsureWithinLimitAsync(userId, tierResult.Value, "plagiarism_check", ct);
        if (!withinLimit.IsSuccess)
            return Result<PlagiarismCheckResult>.Failure(withinLimit.Error ?? "Usage limit reached");

        var plainText = HtmlTextUtility.StripHtml(access.Value.ContentHtml);
        if (plainText.Length < 80)
        {
            return Result<PlagiarismCheckResult>.Failure(
                "Add more content before running a plagiarism check (at least ~80 characters of text).");
        }

        var checkedResult = await provider.CheckTextAsync(plainText, ct);
        if (!checkedResult.IsSuccess || checkedResult.Value is null)
            return Result<PlagiarismCheckResult>.Failure(checkedResult.Error ?? "Plagiarism check failed");

        var entity = new SeoPlagiarismCheck
        {
            Id = Guid.NewGuid(),
            DocumentId = request.DocumentId,
            MatchPercent = checkedResult.Value.MatchPercent,
            MatchesJson = JsonSerializer.Serialize(checkedResult.Value.Matches, JsonOptions),
            CheckedAt = DateTimeOffset.UtcNow,
        };

        var saved = await plagiarism.CreateAsync(entity, ct);
        if (!saved.IsSuccess || saved.Value is null)
            return Result<PlagiarismCheckResult>.Failure(saved.Error ?? "Could not save plagiarism result");

        var incremented = await metering.IncrementAsync(userId, "plagiarism_check", 1, ct);
        if (!incremented.IsSuccess)
            return Result<PlagiarismCheckResult>.Failure(incremented.Error ?? "Usage increment failed");

        return Result<PlagiarismCheckResult>.Success(ToResult(saved.Value, cached: false));
    }

    public async Task<Result<PlagiarismCheckResult?>> GetLatestForDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess)
        {
            return access.Status == ResultStatus.NotFound
                ? Result<PlagiarismCheckResult?>.NotFound(access.Error ?? "Document not found")
                : Result<PlagiarismCheckResult?>.Failure(access.Error ?? "Document access denied");
        }

        var latest = await plagiarism.GetLatestByDocumentAsync(documentId, ct);
        if (!latest.IsSuccess)
            return Result<PlagiarismCheckResult?>.Failure(latest.Error ?? "Lookup failed");

        if (latest.Value is null)
            return Result<PlagiarismCheckResult?>.Success(null);

        var fresh = latest.Value.CheckedAt >= DateTimeOffset.UtcNow.Subtract(CacheTtl);
        return Result<PlagiarismCheckResult?>.Success(ToResult(latest.Value, cached: fresh));
    }

    private async Task<PlagiarismCheckResult?> TryGetFreshCacheAsync(Guid documentId, CancellationToken ct)
    {
        var latest = await plagiarism.GetLatestByDocumentAsync(documentId, ct);
        if (!latest.IsSuccess || latest.Value is null)
            return null;

        if (latest.Value.CheckedAt < DateTimeOffset.UtcNow.Subtract(CacheTtl))
            return null;

        return ToResult(latest.Value, cached: true);
    }

    private static PlagiarismCheckResult ToResult(SeoPlagiarismCheck entity, bool cached) =>
        new(
            entity.Id,
            entity.DocumentId,
            entity.MatchPercent,
            entity.MatchPercent > PublishBlockThresholdPercent,
            cached,
            entity.CheckedAt,
            ParseMatches(entity.MatchesJson));

    private static IReadOnlyList<PlagiarismMatch> ParseMatches(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<PlagiarismMatch>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
