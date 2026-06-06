namespace GeekSeo.Application.Models.Seo;

public static class LocalServiceAreaDefaults
{
    public const int DefaultRadiusMiles = 20;
    public const int MinRadiusMiles = 5;
    public const int MaxRadiusMiles = 100;

    public static int ClampRadiusMiles(int radiusMiles) =>
        Math.Clamp(radiusMiles, MinRadiusMiles, MaxRadiusMiles);
}
