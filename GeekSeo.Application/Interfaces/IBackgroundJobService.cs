using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IBackgroundJobService
{
    Task<Result<BackgroundJobStatus>> GetJobAsync(Guid userId, Guid jobId, CancellationToken ct = default);
}
