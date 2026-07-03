using SiteAnalyzer2.Domain;

namespace SiteAnalyzer2.Services.Integrations;

internal static class CitationLaneQueryHints
{
  private const string Junk =
      "-template -pdf -generator -reddit -quora -course -syllabus";

  internal static string ForLane(string lane, string? keyword)
  {
    var phrase = string.IsNullOrWhiteSpace(keyword)
        ? "\"your keyword\""
        : $"\"{keyword.Trim().Replace("\"", string.Empty, StringComparison.Ordinal)}\"";

    return lane.ToLowerInvariant() switch
    {
      SerpResearchLanes.Wiki =>
          $" Re-run the Google search using: {phrase} site:en.wikipedia.org {Junk} — then save Webpage, HTML only.",
      SerpResearchLanes.Gov =>
          $" Re-run using: {phrase} (site:nist.gov OR site:ftc.gov OR site:usa.gov OR site:cdc.gov OR site:nih.gov) {Junk}.",
      SerpResearchLanes.Edu =>
          $" Re-run using: {phrase} site:edu {Junk}.",
      _ => string.Empty,
    };
  }
}
