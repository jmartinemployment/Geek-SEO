using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Drops SERP domains that only appear for audience-industry pillars (e.g. "Accounting"
/// on a page listing verticals the business serves) rather than core service topics.
/// </summary>
internal static class CompetitorRelevanceFilter
{
  private static readonly HashSet<string> AudienceOnlySources = new(StringComparer.OrdinalIgnoreCase)
  {
    "page_vertical",
    "heading",
    "area_served",
  };

  internal static bool ShouldIncludeCompetitor(
    string domain,
    IReadOnlySet<string> contributingPillarSlugs,
    IReadOnlyDictionary<string, DiscoveredPillar> pillarsBySlug,
    SiteBusinessProfile? siteBusiness)
  {
    if (contributingPillarSlugs.Count == 0)
      return false;

    if (HasCorePillarContribution(contributingPillarSlugs, pillarsBySlug, siteBusiness))
      return true;

    // Only audience-industry pillars contributed — drop obvious wrong-vertical domains.
    foreach (var slug in contributingPillarSlugs)
    {
      if (!pillarsBySlug.TryGetValue(slug, out var pillar))
        continue;

      if (DomainLooksLikeVerticalBusiness(domain, pillar.Name))
        return false;
    }

    return false;
  }

  internal static bool PillarCountsForCompetitorDiscovery(
    DiscoveredPillar pillar,
    SiteBusinessProfile? siteBusiness)
  {
    if (pillar.Source is "schema" or "gsc" or "same_as")
      return true;

    if (AudienceOnlySources.Contains(pillar.Source))
      return false;

    if (siteBusiness is null)
      return true;

    return PillarOverlapsCoreBusiness(pillar.Name, siteBusiness);
  }

  private static bool HasCorePillarContribution(
    IReadOnlySet<string> contributingPillarSlugs,
    IReadOnlyDictionary<string, DiscoveredPillar> pillarsBySlug,
    SiteBusinessProfile? siteBusiness)
  {
    foreach (var slug in contributingPillarSlugs)
    {
      if (!pillarsBySlug.TryGetValue(slug, out var pillar))
        continue;

      if (PillarCountsForCompetitorDiscovery(pillar, siteBusiness))
        return true;
    }

    return false;
  }

  private static bool PillarOverlapsCoreBusiness(string pillarName, SiteBusinessProfile siteBusiness)
  {
    var pillarTokens = Tokenize(pillarName);
    if (pillarTokens.Count == 0)
      return true;

    return pillarTokens.Any(siteBusiness.CoreTopicTokens.Contains);
  }

  private static bool DomainLooksLikeVerticalBusiness(string domain, string verticalName)
  {
    var verticalTokens = Tokenize(verticalName)
      .Where(t => t.Length >= 4)
      .ToList();
    if (verticalTokens.Count == 0)
      return false;

    var domainTokens = TokenizeDomain(domain);
    return verticalTokens.Any(vt =>
      domainTokens.Any(dt =>
        dt.Contains(vt, StringComparison.OrdinalIgnoreCase)
        || vt.Contains(dt, StringComparison.OrdinalIgnoreCase)));
  }

  internal static IReadOnlySet<string> TokenizeDomain(string domain)
  {
    var host = domain.Trim().ToLowerInvariant();
    var slash = host.IndexOf('/');
    if (slash >= 0)
      host = host[..slash];

    host = host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    var dot = host.IndexOf('.');
    if (dot > 0)
      host = host[..dot];

    return Tokenize(host.Replace('.', ' ').Replace('_', ' '));
  }

  internal static HashSet<string> Tokenize(string text)
  {
    var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var part in text.Split(
               [' ', '|', '–', '-', '—', '/', ',', '&', ':', '.', '(', ')'],
               StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      var normalized = part.Trim().ToLowerInvariant();
      if (normalized.Length < 3 || StopWords.Contains(normalized))
        continue;

      tokens.Add(normalized);
    }

    return tokens;
  }

  private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
  {
    "and", "for", "the", "your", "our", "with", "from", "that", "this", "into", "about",
    "services", "service", "solutions", "solution", "company", "business", "south", "north",
    "east", "west", "beach", "city", "county", "florida", "united", "states", "www", "com",
    "net", "org", "biz", "inc", "llc",
  };
}
