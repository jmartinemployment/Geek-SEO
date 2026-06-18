using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ApplySourcesJobService(
    IBackgroundJobRepository jobs,
    IContentDocumentService documents) : IApplySourcesJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<BackgroundJobStatus>> EnqueueAsync(
        Guid userId,
        Guid documentId,
        string keyword,
        string location,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<BackgroundJobStatus>.Failure(access.Error ?? "Access denied");

        var payload = JsonSerializer.Serialize(new ApplySourcesJobPayload
        {
            DocumentId = documentId,
            Keyword = keyword.Trim(),
            Location = string.IsNullOrWhiteSpace(location) ? "United States" : location.Trim(),
        }, JsonOptions);

        var created = await jobs.CreateAsync(new CreateBackgroundJobRequest
        {
            UserId = userId,
            ProjectId = access.Value.ProjectId,
            JobType = IApplySourcesJobService.JobType,
            PayloadJson = payload,
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
