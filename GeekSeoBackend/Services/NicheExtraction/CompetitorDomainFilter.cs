namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Strips high-authority non-competitor domains that pollute SERP results
/// (encyclopedias, social networks, job sites, review directories, government, major cloud vendors).
/// </summary>
public static class CompetitorDomainFilter
{
    private static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Encyclopedias / reference
        "wikipedia.org", "britannica.com", "investopedia.com", "encyclopedia.com",

        // Social / community
        "reddit.com", "linkedin.com", "facebook.com", "twitter.com", "x.com",
        "youtube.com", "instagram.com", "tiktok.com", "quora.com", "medium.com",
        "pinterest.com", "tumblr.com", "nextdoor.com", "alignable.com",

        // Job sites
        "indeed.com", "glassdoor.com", "ziprecruiter.com", "monster.com",
        "careerbuilder.com", "simplyhired.com",

        // Review / directory / aggregator
        "yelp.com", "trustpilot.com", "g2.com", "capterra.com", "angi.com",
        "houzz.com", "thumbtack.com", "homeadvisor.com", "yellowpages.com",
        "bbb.org", "manta.com", "bark.com",

        // Learning platforms
        "coursera.org", "udemy.com", "skillshare.com", "edx.org", "khanacademy.org",
        "pluralsight.com", "lynda.com",

        // Major cloud / enterprise tech (not niche competitors)
        "ibm.com", "oracle.com", "sap.com", "salesforce.com",
        "cloud.google.com", "aws.amazon.com", "azure.microsoft.com", "microsoft.com",

        // News / business media
        "forbes.com", "inc.com", "businessinsider.com", "entrepreneur.com",
        "hbr.org", "wsj.com", "bloomberg.com", "techcrunch.com", "wired.com",

        // Universities / education
        "online.hbs.edu", "harvard.edu", "mit.edu", "stanford.edu",

        // Government
        "bls.gov", "sba.gov", "irs.gov", "ftc.gov",

        // Misc high-DA noise
        "spiderstrategies.com", "accounting.com", "accountingcoach.com",
        "bcg.com", "deloitte.com", "mckinsey.com", "accenture.com",
    };

    private static readonly string[] BlockedTlds = [".gov", ".edu", ".mil"];

    public static bool IsCompetitor(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;

        foreach (var tld in BlockedTlds)
            if (domain.EndsWith(tld, StringComparison.OrdinalIgnoreCase)) return false;

        // Check exact match and subdomains (e.g. cloud.google.com → google.com blocked via exact)
        if (BlockedDomains.Contains(domain)) return false;

        // Check if domain ends with a blocked root (e.g. maps.google.com → google.com)
        foreach (var blocked in BlockedDomains)
            if (domain.EndsWith("." + blocked, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }
}
