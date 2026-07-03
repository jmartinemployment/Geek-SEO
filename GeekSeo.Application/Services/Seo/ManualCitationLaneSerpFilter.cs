using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class ManualCitationLaneSerpFilter : IManualCitationLaneSerpFilter
{
    public IReadOnlyList<SerpOrganicResult> FilterOrganicResults(SerpResult result, string lane) =>
        result.OrganicResults
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .Where(i => CitationLaneHostRules.IsEligibleUrl(i.Url, lane))
            .ToList();
}
