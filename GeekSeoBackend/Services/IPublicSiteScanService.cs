using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public interface IPublicSiteScanService
{
    Task<(bool Ok, PublicScanResponse? Result, string? Error)> ScanAsync(string rawUrl, CancellationToken ct);
}
