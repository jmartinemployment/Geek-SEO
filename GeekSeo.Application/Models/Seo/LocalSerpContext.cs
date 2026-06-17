namespace GeekSeo.Application.Models.Seo;

/// <summary>SERP market string plus optional radius filter from project local settings.</summary>
public sealed record LocalSerpContext(
    string SerpMarketLocation,
    LocalServiceAreaContext? ServiceArea);
