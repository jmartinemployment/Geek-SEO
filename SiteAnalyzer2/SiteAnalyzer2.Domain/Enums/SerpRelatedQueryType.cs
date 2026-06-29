namespace SiteAnalyzer2.Domain.Enums;

/// <summary>
/// Related-query suggestions on a SERP. PASF (People also search for) is treated as PAA —
/// one combined list on the <c>related_searches</c> item in DataForSEO-shaped output.
/// </summary>
public enum SerpRelatedQueryType
{
    RelatedSearch,
    PeopleAlsoAsk = RelatedSearch,
    PeopleAlsoSearchFor = PeopleAlsoAsk,
}
