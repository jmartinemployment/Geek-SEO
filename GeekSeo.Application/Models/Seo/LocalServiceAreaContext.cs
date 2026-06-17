namespace GeekSeo.Application.Models.Seo;

/// <summary>Geocoded business center + service radius for local competitor filtering.</summary>
public sealed record LocalServiceAreaContext(
    double CenterLatitude,
    double CenterLongitude,
    int RadiusMiles);
