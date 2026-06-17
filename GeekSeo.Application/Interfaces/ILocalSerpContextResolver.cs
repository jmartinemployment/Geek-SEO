using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces;

public interface ILocalSerpContextResolver
{
    Task<Result<LocalSerpContext>> ResolveAsync(Guid projectId, CancellationToken ct = default);
}
