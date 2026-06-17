using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticleMethodologyDraftEnricher
{
    public static string EnsureMethodologyDraft(string draft, ContentBrief brief) =>
        ArticleMethodologyScaffold.EnsureBodySections(
            draft,
            brief.Keyword,
            brief.Methodology);
}
