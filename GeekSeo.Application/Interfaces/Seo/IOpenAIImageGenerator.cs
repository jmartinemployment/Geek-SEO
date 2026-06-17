using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IOpenAIImageGenerator
{
    Task<Result<FeaturedImageResult>> GenerateAsync(string prompt, CancellationToken ct = default);
}
