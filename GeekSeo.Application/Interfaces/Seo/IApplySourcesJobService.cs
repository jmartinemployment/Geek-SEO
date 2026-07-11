using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IApplySourcesJobService
{
    public const string JobType = "apply_sources";

    Task<Result<BackgroundJobStatus>> EnqueueAsync(
        Guid userId,
        Guid documentId,
        string keyword,
        string location,
        CancellationToken ct = default);
}
