namespace GeekSeo.Application.Models.Seo;

/// <summary>Normalized GSC query + landing page row for topical clustering.</summary>
public sealed record GscQueryRow(
    string Query,
    string Page,
    long Impressions,
    long Clicks,
    double Position);
