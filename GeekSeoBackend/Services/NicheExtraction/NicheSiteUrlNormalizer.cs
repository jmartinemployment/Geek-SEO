using GeekSeo.Application.Infrastructure;

namespace GeekSeoBackend.Services.NicheExtraction;

internal static class NicheSiteUrlNormalizer
{
    public static string Normalize(string raw) => SeoSiteUrlNormalizer.Normalize(raw);
}
