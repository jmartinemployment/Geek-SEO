using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IBrandVoiceService
{
    Task<Result<IReadOnlyList<BrandVoiceDto>>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<Result<BrandVoiceDto>> GetAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<Result<BrandVoiceDto>> CreateAsync(Guid userId, CreateBrandVoiceRequest request, CancellationToken ct = default);
    Task<Result<BrandVoiceDto>> UpdateAsync(Guid userId, Guid id, UpdateBrandVoiceRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
