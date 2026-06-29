namespace SiteAnalyzer2.Domain.Entities;

public class SerpItemHighlighted
{
    public Guid Id { get; set; }
    public Guid SerpItemId { get; set; }
    public int Sequence { get; set; }
    public string Text { get; set; } = string.Empty;

    public SerpItem SerpItem { get; set; } = null!;
}
