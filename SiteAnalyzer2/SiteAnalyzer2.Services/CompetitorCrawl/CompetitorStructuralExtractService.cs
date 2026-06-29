using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public class CompetitorStructuralExtractService(PageExtractionService extractionService)
{
    public void ApplyStructuralExtraction(CompetitorPage page, string html)
    {
        if (!Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUrl))
            return;

        var domain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(page.Url));
        var extraction = extractionService.Extract(html, pageUrl, domain);

        page.CanonicalUrl = extraction.CanonicalUrl;

        foreach (var heading in extraction.Headings)
        {
            page.Headings.Add(new CompetitorPageHeading
            {
                Id = Guid.NewGuid(),
                ProjectId = page.ProjectId,
                CompetitorPageId = page.Id,
                Level = heading.Level,
                Text = heading.Text,
                Sequence = heading.Sequence
            });
        }

        foreach (var meta in extraction.MetaTags)
        {
            page.MetaTags.Add(new CompetitorPageMetaTag
            {
                Id = Guid.NewGuid(),
                ProjectId = page.ProjectId,
                CompetitorPageId = page.Id,
                NameOrProperty = meta.NameOrProperty,
                Content = meta.Content
            });
        }

        foreach (var block in extraction.JsonLdBlocks)
        {
            page.JsonLdBlocks.Add(new CompetitorPageJsonLd
            {
                Id = Guid.NewGuid(),
                ProjectId = page.ProjectId,
                CompetitorPageId = page.Id,
                RawJson = block.RawJson,
                ParsedType = block.ParsedType
            });
        }
    }
}
