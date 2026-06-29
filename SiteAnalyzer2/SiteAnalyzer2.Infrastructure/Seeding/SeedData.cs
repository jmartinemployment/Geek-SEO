using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Infrastructure.Seeding;

public static class SeedData
{
    public static readonly string[] ReferenceExcludeDomains =
    [
        "wikipedia.org", "en.wikipedia.org", "britannica.com", "dictionary.com",
        "merriam-webster.com", "thesaurus.com", "investopedia.com", "webmd.com",
        "mayoclinic.org", "nih.gov", "cdc.gov", "reuters.com", "apnews.com",
        "bbc.com", "nytimes.com", "theguardian.com", "forbes.com", "techcrunch.com",
        "wired.com", "archive.org"
    ];

    public static readonly string[] KnownPlatformDomains =
    [
        "reddit.com", "www.reddit.com", "old.reddit.com",
        "quora.com", "www.quora.com",
        "youtube.com", "www.youtube.com",
        "stackoverflow.com", "stackexchange.com",
        "medium.com", "www.medium.com",
        "producthunt.com"
    ];

    public static readonly string[] CrawlPriorityUrlPatterns =
    [
        "/about", "/about-us", "/services", "/what-we-do", "/contact", "/team", "/who-we-are"
    ];

    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.ReferenceExcludeDomains.AnyAsync(ct))
        {
            db.ReferenceExcludeDomains.AddRange(
                ReferenceExcludeDomains.Select(d => new ReferenceExcludeDomain { Domain = d }));
        }

        if (!await db.KnownPlatformDomains.AnyAsync(ct))
        {
            db.KnownPlatformDomains.AddRange(
                KnownPlatformDomains.Select(d => new KnownPlatformDomain { Domain = d }));
        }

        if (!await db.CrawlPriorityUrlPatterns.AnyAsync(ct))
        {
            db.CrawlPriorityUrlPatterns.AddRange(
                CrawlPriorityUrlPatterns.Select(p => new CrawlPriorityUrlPattern
                {
                    Id = Guid.NewGuid(),
                    Pattern = p
                }));
        }

        await db.SaveChangesAsync(ct);
    }
}
