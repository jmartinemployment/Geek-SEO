namespace GeekSeo.Application.Models.Seo;

/// <summary>Google Maps / local pack entry with optional coordinates for radius filtering.</summary>
public sealed record SerpLocalPlace(
    string Domain,
    double? Latitude,
    double? Longitude);
