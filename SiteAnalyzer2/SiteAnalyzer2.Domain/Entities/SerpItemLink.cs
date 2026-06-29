namespace SiteAnalyzer2.Domain.Entities;

public class SerpItemLink
{
    public Guid Id { get; set; }
    public Guid SerpItemId { get; set; }
    public int Sequence { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public SerpItem SerpItem { get; set; } = null!;
}
