using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentDraftJobService(
    IBackgroundJobRepository jobs,
    IContentDocumentService documents) : IContentDraftJobService
{
    public const string JobType = "content_draft";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<BackgroundJobStatus>> EnqueueKeywordDraftAsync(
        Guid userId, Guid documentId, KeywordContentDraftRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result<BackgroundJobStatus>.Failure("Keyword is required");

        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<BackgroundJobStatus>.Failure(access.Error ?? "Access denied");

        var title = string.IsNullOrWhiteSpace(request.Title) ? request.Keyword.Trim() : request.Title.Trim();
        var payload = JsonSerializer.Serialize(new ContentDraftJobPayload
        {
            DocumentId = documentId,
            Mode = "keyword",
            Keyword = request.Keyword.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? "United States" : request.Location.Trim(),
            Title = title,
        }, JsonOptions);

        return await CreateJobAsync(userId, access.Value.ProjectId, payload, ct);
    }

    public async Task<Result<BackgroundJobStatus>> EnqueueResearchDraftAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<BackgroundJobStatus>.Failure(access.Error ?? "Access denied");

        if (!ResearchBackedWriteGate.IsResearchBacked(access.Value))
            return Result<BackgroundJobStatus>.Failure("Attach page research from URL Analyzer first.");

        var payload = JsonSerializer.Serialize(new ContentDraftJobPayload
        {
            DocumentId = documentId,
            Mode = "research",
        }, JsonOptions);

        return await CreateJobAsync(userId, access.Value.ProjectId, payload, ct);
    }

    private async Task<Result<BackgroundJobStatus>> CreateJobAsync(
        Guid userId, Guid projectId, string payloadJson, CancellationToken ct)
    {
        var created = await jobs.CreateAsync(new CreateBackgroundJobRequest
        {
            UserId = userId,
            ProjectId = projectId,
            JobType = JobType,
            PayloadJson = payloadJson,
        }, ct);

        if (!created.IsSuccess || created.Value is null)
            return Result<BackgroundJobStatus>.Failure(created.Error ?? "Failed to create job");

        var job = created.Value;
        return Result<BackgroundJobStatus>.Success(new BackgroundJobStatus
        {
            JobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ProgressPercent = job.ProgressPercent,
        });
    }
}
