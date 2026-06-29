using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentDocumentKindResolver
{
    public static string Resolve(string? requested, Guid? parentDocumentId)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested.Trim();

        return parentDocumentId is not null
            ? ContentDocumentKinds.Spoke
            : ContentDocumentKinds.Standalone;
    }
}
