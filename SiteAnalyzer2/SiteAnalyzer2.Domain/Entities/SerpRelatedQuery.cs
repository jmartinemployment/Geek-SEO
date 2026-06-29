using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class SerpRelatedQuery
{
    public Guid Id { get; set; }
    public Guid SerpItemId { get; set; }
    public int Sequence { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public SerpRelatedQueryType QueryType { get; set; } = SerpRelatedQueryType.RelatedSearch;

    public SerpItem SerpItem { get; set; } = null!;
}
