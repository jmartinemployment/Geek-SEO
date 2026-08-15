namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Content Creator read model for one crawled page. Headings are the queryable
/// assignment index; <see cref="MainContentMarkdown"/> is the LLM body.
/// </summary>
public sealed class PageContext
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Headings { get; set; } = [];
    public string MainContentMarkdown { get; set; } = "";
}
