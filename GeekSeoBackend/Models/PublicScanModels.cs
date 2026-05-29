namespace GeekSeoBackend.Models;

public sealed record PublicScanResponse(
    string Url,
    int? PerformanceScore,
    int? SeoScore,
    int? AccessibilityScore,
    string? Lcp,
    string? Cls,
    string? Inp,
    string? Title,
    string? MetaDescription,
    string? H1,
    string? Canonical,
    bool? RobotsTxtFound,
    bool PageSpeedAvailable,
    IReadOnlyList<string> NextSteps);

public sealed record PublicScanErrorResponse(string Error);
