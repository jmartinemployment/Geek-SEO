using GeekSeo.Application.Infrastructure;

namespace GeekSeoBackend.Services.SiteExtraction;

internal static class SiteUrlNormalizer
{
    public static string Normalize(string raw) => SeoSiteUrlNormalizer.Normalize(raw);
}
