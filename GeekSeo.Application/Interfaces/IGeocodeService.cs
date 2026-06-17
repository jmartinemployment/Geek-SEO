using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces;

public interface IGeocodeService
{
    Task<Result<GeoCoordinate>> GeocodeAsync(string addressQuery, CancellationToken ct = default);
}
