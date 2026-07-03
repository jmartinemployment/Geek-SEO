using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Interfaces.Seo;

/// <summary>
/// Filters live <see cref="SerpResult"/> organics for manual citation lanes (wiki, gov, edu).
/// Complements <see cref="ISerpProvider"/> — provider fetches; this validates lane eligibility.
/// </summary>
public interface IManualCitationLaneSerpFilter
{
    IReadOnlyList<SerpOrganicResult> FilterOrganicResults(SerpResult result, string lane);
}
