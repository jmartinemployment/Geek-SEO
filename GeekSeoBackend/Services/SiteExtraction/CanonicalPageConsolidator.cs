using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Index-time consolidation of pages that are the same document under different URLs.
/// <para>
/// <see cref="CrawledPage.Canonical"/> is captured at fetch time but non-canonical pages are kept
/// ("consolidation is index-time"). This is that step: before trees are built and persisted,
/// collapse each <c>rel="canonical"</c> group to one page keyed by the canonical URL — the same
/// thing a search engine does rather than indexing both.
/// </para>
/// <para>
/// Without it, a site serving 200 on both <c>www.</c> and apex produces two identical tree rows,
/// so every hierarchy match appears twice and downstream has to hide the duplicate.
/// </para>
/// </summary>
internal static class CanonicalPageConsolidator
{
    /// <summary>
    /// One page per canonical document, with <see cref="CrawledPage.Url"/> rewritten to the
    /// canonical URL so persisted rows and <c>sourcePageUrl</c> agree.
    /// </summary>
    public static IReadOnlyList<CrawledPage> Consolidate(IReadOnlyList<CrawledPage> pages)
    {
        if (pages.Count <= 1)
            return pages;

        // Keep the page's own fetched URL beside it: once Url is rewritten to the canonical, it
        // can no longer be compared against the canonical to decide which copy to keep.
        var byKey = new Dictionary<string, (CrawledPage Page, string FetchedUrl)>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        foreach (var page in pages)
        {
            var canonical = ResolveCanonical(page);
            var key = CrawlUrl.Canonicalize(TrimTrailingSlash(canonical));
            var fetchedUrl = string.IsNullOrWhiteSpace(page.FinalUrl) ? page.Url : page.FinalUrl;

            if (!byKey.TryGetValue(key, out var existing))
            {
                byKey[key] = (page with { Url = canonical }, fetchedUrl);
                order.Add(key);
                continue;
            }

            // Prefer the page actually fetched at the canonical URL; it is the one whose HTML the
            // canonical tag points at. Otherwise keep the richer document.
            if (Prefer(page, fetchedUrl, existing, canonical))
                byKey[key] = (page with { Url = canonical }, fetchedUrl);
        }

        return order.ConvertAll(k => byKey[k].Page);
    }

    private static bool Prefer(
        CrawledPage candidate, string candidateFetchedUrl,
        (CrawledPage Page, string FetchedUrl) existing, string canonical)
    {
        var candidateIsSelf = SameUrl(candidateFetchedUrl, canonical);
        var existingIsSelf = SameUrl(existing.FetchedUrl, canonical);
        if (candidateIsSelf != existingIsSelf)
            return candidateIsSelf;

        if (candidate.HasDocument != existing.Page.HasDocument)
            return candidate.HasDocument;

        return (candidate.Html?.Length ?? 0) > (existing.Page.Html?.Length ?? 0);
    }

    /// <summary>
    /// The page's own URL unless it declares a same-site canonical. Cross-site canonicals are
    /// ignored — a page cannot hand its identity to another domain in this crawl.
    /// </summary>
    private static string ResolveCanonical(CrawledPage page)
    {
        var own = string.IsNullOrWhiteSpace(page.FinalUrl) ? page.Url : page.FinalUrl;
        var declared = (page.Canonical ?? "").Trim();
        if (declared.Length == 0)
            return own;

        if (!Uri.TryCreate(own, UriKind.Absolute, out var ownUri))
            return own;

        if (!Uri.TryCreate(declared, UriKind.Absolute, out var canonicalUri)
            && !Uri.TryCreate(ownUri, declared, out canonicalUri))
            return own;

        return CrawlUrl.IsSameSite(canonicalUri, ownUri) ? canonicalUri.ToString() : own;
    }

    private static bool SameUrl(string a, string b) =>
        string.Equals(
            CrawlUrl.Canonicalize(TrimTrailingSlash(a)),
            CrawlUrl.Canonicalize(TrimTrailingSlash(b)),
            StringComparison.OrdinalIgnoreCase);

    private static string TrimTrailingSlash(string url)
    {
        var stripped = CrawlUrl.StripFragment(url ?? "").TrimEnd();
        if (stripped.Length <= 1) return stripped;
        // Keep "https://host/" meaningful but treat "/path/" and "/path" as one.
        return stripped.EndsWith('/') && !stripped.EndsWith("://", StringComparison.Ordinal)
            ? stripped.TrimEnd('/')
            : stripped;
    }
}
