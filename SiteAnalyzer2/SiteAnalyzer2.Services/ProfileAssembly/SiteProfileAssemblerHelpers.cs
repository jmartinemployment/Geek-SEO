using System.Text.Json;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.ProfileAssembly;

public static class SiteProfileAssemblerHelpers
{
    private static readonly string[] PreferredBusinessSchemaTypes =
    [
        "LocalBusiness", "ProfessionalService", "Organization", "Corporation",
        "Store", "Restaurant", "MedicalBusiness", "LegalService",
    ];

    /// <summary>Frase quality bar — appended to run-level writing instructions for Content Writer.</summary>
    public const string ContentQualityBarInstruction =
        "Quality bar: fully answer this pillar better than the seed pages; cite named sources for factual claims; human-edit AI drafts before publish.";

    public static string? FindMatchedPillarTopic(string keyword, IReadOnlyList<string> headingTexts)
    {
        if (headingTexts.Count == 0)
            return string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        var normalizedKeyword = keyword.Trim().ToLowerInvariant();
        var direct = headingTexts.FirstOrDefault(h =>
        {
            var text = h.Trim().ToLowerInvariant();
            return text.Contains(normalizedKeyword, StringComparison.Ordinal)
                || normalizedKeyword.Contains(text, StringComparison.Ordinal)
                || text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(token => token.Length > 3 && normalizedKeyword.Contains(token, StringComparison.Ordinal));
        });

        return direct ?? headingTexts[0];
    }

    public static string InferSearchIntent(IReadOnlyList<SerpItem> serpItems, string keyword)
    {
        var hasLocalPack = serpItems.Any(i =>
            string.Equals(i.Type, "local_pack", StringComparison.OrdinalIgnoreCase));
        if (hasLocalPack)
            return "local";

        var paidCount = serpItems.Count(i => i.Ads || i.Type == SerpItemTypes.Paid);
        var organicCount = serpItems.Count(i => i.Type == SerpItemTypes.Organic && !i.Ads);
        if (paidCount > 0 && paidCount >= organicCount / 3)
            return "commercial";

        return InferIntentFromKeyword(keyword);
    }

    public static string InferIntentFromKeyword(string keyword)
    {
        var normalized = keyword.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            return "informational";

        if (normalized.Contains("near me", StringComparison.Ordinal) || normalized.Contains(" in ", StringComparison.Ordinal))
            return "local";

        if (normalized.StartsWith("how ", StringComparison.Ordinal)
            || normalized.StartsWith("what ", StringComparison.Ordinal)
            || normalized.StartsWith("why ", StringComparison.Ordinal))
            return "informational";

        if (normalized.Contains("best ", StringComparison.Ordinal)
            || normalized.Contains(" top ", StringComparison.Ordinal)
            || normalized.Contains(" vs ", StringComparison.Ordinal))
            return "commercial";

        return "informational";
    }

    public static IReadOnlyList<string> ExtractGeoAnchorNodes(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        var nodes = new List<string>();
        foreach (var block in jsonLdBlocks)
        {
            nodes.AddRange(ParseGeoNodesFromJsonLd(block.RawJson));
        }

        return nodes
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    public static string? ExtractServiceAreaDescription(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        foreach (var block in PrioritizeBusinessJsonLd(jsonLdBlocks))
        {
            var area = FormatJsonLdAreaServed(block.RawJson);
            if (!string.IsNullOrWhiteSpace(area))
                return area;
        }

        return null;
    }

    public static string? ExtractBusinessType(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        foreach (var block in PrioritizeBusinessJsonLd(jsonLdBlocks))
        {
            var types = ParseJsonLdTypes(block.RawJson);
            if (types.Count > 0)
                return FormatBusinessTypeLabel(types);

            if (!string.IsNullOrWhiteSpace(block.ParsedType))
                return FormatBusinessTypeLabel([block.ParsedType.Trim()]);
        }

        return null;
    }

    public static string? ExtractBusinessName(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        foreach (var block in PrioritizeBusinessJsonLd(jsonLdBlocks))
        {
            var name = ParseJsonLdStringProperty(block.RawJson, "name");
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();
        }

        return null;
    }

    public static string? ExtractBusinessDescription(
        TargetPageSnapshot? homepage,
        IReadOnlyList<PageJsonLd> allJsonLd)
    {
        var candidates = new List<string>();

        foreach (var block in PrioritizeBusinessJsonLd(allJsonLd))
        {
            var description = ParseJsonLdStringProperty(block.RawJson, "description");
            if (!string.IsNullOrWhiteSpace(description))
                candidates.Add(description.Trim());
        }

        var metaDescription = homepage?.MetaTags
            .FirstOrDefault(m => m.NameOrProperty.Equals("description", StringComparison.OrdinalIgnoreCase))
            ?.Content;
        if (!string.IsNullOrWhiteSpace(metaDescription))
            candidates.Add(metaDescription.Trim());

        var ogDescription = homepage?.MetaTags
            .FirstOrDefault(m => m.NameOrProperty.Equals("og:description", StringComparison.OrdinalIgnoreCase))
            ?.Content;
        if (!string.IsNullOrWhiteSpace(ogDescription))
            candidates.Add(ogDescription.Trim());

        var title = homepage?.MetaTags
            .FirstOrDefault(m => m.NameOrProperty.Equals("og:title", StringComparison.OrdinalIgnoreCase)
                || m.NameOrProperty.Equals("title", StringComparison.OrdinalIgnoreCase))
            ?.Content;

        return candidates
            .OrderByDescending(c => c.Length)
            .FirstOrDefault();
    }

    public static string? ExtractPageTitle(TargetPageSnapshot? homepage) =>
        homepage?.MetaTags
            .FirstOrDefault(m => m.NameOrProperty.Equals("og:title", StringComparison.OrdinalIgnoreCase))
            ?.Content?.Trim()
        ?? homepage?.MetaTags
            .FirstOrDefault(m => m.NameOrProperty.Equals("title", StringComparison.OrdinalIgnoreCase))
            ?.Content?.Trim();

    public static IReadOnlyList<string> ExtractCompetitorDomains(
        IReadOnlyList<SerpItem> serpItems,
        string targetSiteUrl)
    {
        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(targetSiteUrl));
        return serpItems
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads && !string.IsNullOrWhiteSpace(i.Domain))
            .Select(i => i.Domain!.Trim().ToLowerInvariant())
            .Where(d => !DomainHelper.HostsMatch(d, targetDomain))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    public static IReadOnlyList<string> ExtractAuthorityPageUrls(IReadOnlyList<SerpItem> serpItems) =>
        serpItems
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads && !string.IsNullOrWhiteSpace(i.Url))
            .OrderBy(i => i.RankAbsolute)
            .Select(i => i.Url!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

    public static IReadOnlyList<string> BuildNicheTags(IReadOnlyList<SerpItem> serpItems) =>
        ExtractRelatedQueries(serpItems)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

    public static IReadOnlyList<string> BuildNicheTagsFromHomepage(
        IReadOnlyList<string> headingTexts,
        IReadOnlyList<PageJsonLd> jsonLdBlocks,
        string? businessType,
        string displayName)
    {
        var tags = new List<string>();
        tags.AddRange(
            headingTexts
                .Where(h => !string.IsNullOrWhiteSpace(h) && h.Trim().Length > 2 && !LooksLikeBrokenHeadingTag(h.Trim()))
                .Select(h => h.Trim())
                .Take(12));
        tags.AddRange(ExtractTopicTagsFromJsonLd(jsonLdBlocks));

        if (!string.IsNullOrWhiteSpace(businessType))
        {
            foreach (var part in businessType.Split('·', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                tags.Add(part);
        }

        if (!string.IsNullOrWhiteSpace(displayName))
            tags.Add(displayName.Trim());

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToList();
    }

    public static string BuildHomepageNicheDescription(
        string displayName,
        string? businessDescription,
        string? serviceAreaDescription)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(businessDescription))
            parts.Add(businessDescription.Trim());

        if (!string.IsNullOrWhiteSpace(serviceAreaDescription)
            && !string.Equals(serviceAreaDescription.Trim(), businessDescription?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"Service area: {serviceAreaDescription.Trim()}.");
        }

        if (parts.Count == 0)
            return $"Business site for {displayName.Trim()}.";

        return string.Join(" ", parts);
    }

    public static string BuildHomepagePrimaryNiche(
        string displayName,
        string? businessName,
        string? businessType,
        string? pageTitle)
    {
        if (!string.IsNullOrWhiteSpace(businessName) && !string.IsNullOrWhiteSpace(businessType))
            return $"{businessName.Trim()} · {businessType.Trim()}";

        if (!string.IsNullOrWhiteSpace(pageTitle))
            return pageTitle.Trim();

        if (!string.IsNullOrWhiteSpace(businessName))
            return businessName.Trim();

        if (!string.IsNullOrWhiteSpace(businessType))
            return businessType.Trim();

        return displayName.Trim();
    }

    public static string BuildHomepageBusinessSummary(
        string? businessName,
        string? businessDescription,
        string? serviceAreaDescription,
        IReadOnlyList<string> geoAnchorNodes)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(businessName))
            parts.Add(businessName.Trim());

        if (!string.IsNullOrWhiteSpace(businessDescription))
            parts.Add(businessDescription.Trim());

        if (!string.IsNullOrWhiteSpace(serviceAreaDescription))
            parts.Add($"Serves {serviceAreaDescription.Trim()}.");

        if (geoAnchorNodes.Count > 0)
            parts.Add($"Based in {string.Join(", ", geoAnchorNodes.Take(3))}.");

        return string.Join(" ", parts).Trim();
    }

    public static SiteProfileAssemblyWrite BuildSiteProfileFromHomepage(
        TargetPageSnapshot homepage,
        string displayName)
    {
        var allJsonLd = homepage.JsonLdBlocks.ToList();
        var businessName = ExtractBusinessName(allJsonLd);
        var businessType = ExtractBusinessType(allJsonLd);
        var businessDescription = ExtractBusinessDescription(homepage, allJsonLd);
        var pageTitle = ExtractPageTitle(homepage);
        var geoNodes = ExtractGeoAnchorNodes(allJsonLd);
        var serviceArea = ExtractServiceAreaDescription(allJsonLd);
        var headingTexts = homepage.Headings
            .Where(h => h.Level <= 3)
            .OrderBy(h => h.Sequence)
            .Select(h => h.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        var nicheTags = BuildNicheTagsFromHomepage(headingTexts, allJsonLd, businessType, displayName);
        var nicheDescription = BuildHomepageNicheDescription(displayName, businessDescription, serviceArea);
        var businessSummary = BuildHomepageBusinessSummary(businessName, businessDescription, serviceArea, geoNodes);
        var writingRecommendations = BuildHomepageWritingRecommendations(
            homepage,
            businessType,
            serviceArea,
            geoNodes,
            nicheTags,
            businessDescription);

        return new SiteProfileAssemblyWrite
        {
            BusinessType = businessType,
            BusinessDescription = businessDescription,
            BusinessSummary = string.IsNullOrWhiteSpace(businessSummary) ? businessDescription : businessSummary,
            ServiceAreaDescription = serviceArea,
            GeoAnchorNodes = geoNodes,
            PrimaryNiche = BuildHomepagePrimaryNiche(displayName, businessName, businessType, pageTitle),
            NicheDescription = nicheDescription,
            NicheTags = nicheTags,
            CompetitorDomains = [],
            AuthorityPageUrls = [],
            WritingRecommendations = writingRecommendations,
        };
    }

    public static IReadOnlyList<string> BuildHomepageWritingRecommendations(
        TargetPageSnapshot homepage,
        string? businessType,
        string? serviceAreaDescription,
        IReadOnlyList<string> geoAnchorNodes,
        IReadOnlyList<string> nicheTags,
        string? businessDescription)
    {
        var recommendations = new List<string>();
        var useCasePages = ExtractUseCasePages(homepage.InternalLinks);
        var geoLabel = FormatGeoRecommendationLabel(serviceAreaDescription, geoAnchorNodes);
        var sellsImplementation = LooksLikeImplementationConsultancy(businessType, businessDescription);

        recommendations.Add(
            "JSON-LD: paste the Business entity and WebSite blocks from the Site profile panel into your homepage <head>. "
            + "Content Writer adds TechArticle on each content page — do not merge article schema into the business block.");

        if (sellsImplementation && !string.IsNullOrWhiteSpace(geoLabel))
        {
            recommendations.Add(
                $"Positioning: you sell AI implementation for SMBs in {geoLabel}. "
                + "Reframe existing content pages toward local consulting and deployment — rewrite in place; only add a new page when keyword research reveals a true gap.");
        }
        else if (sellsImplementation)
        {
            recommendations.Add(
                "Positioning: reframe existing content pages toward SMB AI implementation and consulting — rewrite in place rather than publishing parallel generic AI thought-leadership pages.");
        }

        if (useCasePages.Count > 0)
        {
            var pageLabels = useCasePages
                .Select(p => p.Label)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            recommendations.Add(
                $"Existing pillars detected ({string.Join(", ", pageLabels)}): strengthen these pages with local proof, assessment CTAs, and implementation steps — not duplicate URLs.");

            foreach (var page in useCasePages.Where(p => p.IsMarketingThoughtLeadership).Take(3))
            {
                recommendations.Add(
                    $"Reposition \"{page.Label}\" ({page.Path}): shift from generic AI-industry copy to how you implement this capability for local SMB clients.");
            }
        }
        else
        {
            recommendations.Add(
                "After keyword import, align each content page to a service pillar and your geographic market before expanding the site map.");
        }

        if (nicheTags.Any(LooksLikeBrokenHeadingTag))
        {
            recommendations.Add(
                "Homepage headings are merging words (e.g. ArtificialIntelligence, Clone YourselfWork 24/7). Fix hero H2 spacing/CSS so profile tags and on-page copy extract cleanly.");
        }

        return recommendations
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    public static IReadOnlyList<string> BuildRunWritingRecommendations(
        SiteProfileAssemblyWrite siteWrite,
        RunWritingFocusWrite runWrite,
        string keyword)
    {
        var recommendations = new List<string>(siteWrite.WritingRecommendations);

        if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarTopic))
        {
            var line = $"Keyword \"{keyword.Trim()}\": align with pillar \"{runWrite.MatchedPillarTopic.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarIntent))
                line += $" ({runWrite.MatchedPillarIntent.Trim()} intent)";
            if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarAngle))
                line += $". Angle: {runWrite.MatchedPillarAngle.Trim()}";
            recommendations.Add(line + ".");
        }

        if (runWrite.GapTopics.Count > 0)
        {
            recommendations.Add(
                $"Content gaps for \"{keyword.Trim()}\": {string.Join(", ", runWrite.GapTopics.Take(5))}.");
        }

        if (!string.IsNullOrWhiteSpace(siteWrite.ServiceAreaDescription))
        {
            recommendations.Add(
                $"Localize copy for {siteWrite.ServiceAreaDescription.Trim()} — implementation consulting, not national SaaS positioning.");
        }

        return recommendations
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private sealed record UseCasePageRef(string Label, string Path, bool IsMarketingThoughtLeadership);

    private static List<UseCasePageRef> ExtractUseCasePages(IReadOnlyList<TargetPageInternalLink> internalLinks)
    {
        var pages = new List<UseCasePageRef>();
        foreach (var link in internalLinks)
        {
            if (!Uri.TryCreate(link.AbsoluteUrl, UriKind.Absolute, out var uri))
                continue;

            var path = uri.AbsolutePath;
            if (!path.Contains("/use-cases/", StringComparison.OrdinalIgnoreCase))
                continue;

            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                continue;

            var slug = segments[^1];
            if (slug.Equals("use-cases", StringComparison.OrdinalIgnoreCase))
                continue;

            var label = HumanizeSlug(slug);
            if (!string.IsNullOrWhiteSpace(link.AnchorText) && link.AnchorText.Trim().Length > 2)
                label = link.AnchorText.Trim();

            var marketingTopics = new[]
            {
                "content marketing", "customer journey", "customer journeys", "market intelligence",
                "content operations", "seo", "copywriting",
            };

            var isMarketing = marketingTopics.Any(topic =>
                label.Contains(topic, StringComparison.OrdinalIgnoreCase)
                || slug.Contains(topic.Replace(" ", "-"), StringComparison.OrdinalIgnoreCase));

            pages.Add(new UseCasePageRef(label, path, isMarketing));
        }

        return pages
            .GroupBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string HumanizeSlug(string slug) =>
        slug.Replace('-', ' ').Trim();

    private static bool LooksLikeImplementationConsultancy(string? businessType, string? businessDescription)
    {
        var text = $"{businessType} {businessDescription}".ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("consult", StringComparison.Ordinal)
            || text.Contains("implement", StringComparison.Ordinal)
            || text.Contains("integrat", StringComparison.Ordinal)
            || text.Contains("automation", StringComparison.Ordinal)
            || text.Contains("small business", StringComparison.Ordinal);
    }

    private static string? FormatGeoRecommendationLabel(
        string? serviceAreaDescription,
        IReadOnlyList<string> geoAnchorNodes)
    {
        if (!string.IsNullOrWhiteSpace(serviceAreaDescription))
            return serviceAreaDescription.Trim();

        if (geoAnchorNodes.Count > 0)
            return string.Join(", ", geoAnchorNodes.Take(3));

        return null;
    }

    private static bool LooksLikeBrokenHeadingTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag.Contains(' '))
            return false;

        var upperTransitions = 0;
        for (var i = 1; i < tag.Length; i++)
        {
            if (char.IsUpper(tag[i]) && char.IsLower(tag[i - 1]))
                upperTransitions++;
        }

        return upperTransitions >= 2 || tag.Contains("Your", StringComparison.Ordinal) && tag.Length > 18;
    }

    public static void ValidateHomepageOutput(SiteProfileAssemblyWrite siteWrite)
    {
        if (string.IsNullOrWhiteSpace(siteWrite.BusinessType)
            && string.IsNullOrWhiteSpace(siteWrite.BusinessDescription))
        {
            throw new InvalidOperationException(
                "Could not derive business type or description from the site homepage (JSON-LD or meta description).");
        }

        if (siteWrite.NicheTags.Count == 0)
        {
            throw new InvalidOperationException(
                "Could not derive niche tags from the site homepage headings or business metadata.");
        }
    }

    public static SiteProfileAssemblyWrite BuildSiteProfileFromRunSource(SiteProfileAssemblySource source)
    {
        var allJsonLd = source.TargetPages.SelectMany(p => p.JsonLdBlocks).ToList();
        var homepage = source.TargetPages.FirstOrDefault();
        var keyword = source.Run.Keyword.Trim();
        var competitorDomains = ExtractCompetitorDomains(source.SerpItems, source.SiteProfile.SiteUrl);
        var businessType = ExtractBusinessType(allJsonLd);
        var businessDescription = ExtractBusinessDescription(homepage, allJsonLd);
        var geoNodes = ExtractGeoAnchorNodes(allJsonLd);
        var serviceArea = ExtractServiceAreaDescription(allJsonLd);
        var nicheTags = source.SiteProfile.NicheTags.Count > 0
            ? source.SiteProfile.NicheTags
            : BuildNicheTags(source.SerpItems);

        return new SiteProfileAssemblyWrite
        {
            BusinessType = businessType,
            BusinessDescription = businessDescription,
            BusinessSummary = businessDescription,
            ServiceAreaDescription = serviceArea,
            GeoAnchorNodes = geoNodes,
            PrimaryNiche = BuildPrimaryNiche(keyword, competitorDomains, businessType),
            NicheDescription = BuildNicheDescription(keyword, source.SerpItems),
            NicheTags = nicheTags,
            CompetitorDomains = competitorDomains,
            AuthorityPageUrls = ExtractAuthorityPageUrls(source.SerpItems),
        };
    }

    public static string BuildNicheDescription(string keyword, IReadOnlyList<SerpItem> serpItems)
    {
        var snippets = serpItems
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads && !string.IsNullOrWhiteSpace(i.Description))
            .OrderBy(i => i.RankAbsolute)
            .Take(3)
            .Select(i => i.Description!.Trim())
            .ToList();

        if (snippets.Count == 0)
            return $"Search landscape for \"{keyword}\".";

        return $"Keyword space for \"{keyword}\": {string.Join(" ", snippets)}";
    }

    public static string BuildPrimaryNiche(string keyword, IReadOnlyList<string> competitorDomains, string? businessType)
    {
        if (!string.IsNullOrWhiteSpace(businessType))
            return businessType.Trim();

        if (competitorDomains.Count > 0)
            return $"{keyword.Trim()} (vs. {string.Join(", ", competitorDomains.Take(3))})";

        return keyword.Trim();
    }

    public static IReadOnlyList<string> BuildGapTopics(
        IReadOnlyList<Finding> gapFindings,
        IReadOnlyList<SerpItem> serpItems,
        string keyword) =>
        BuildGapTopicsFromResearch(gapFindings, serpItems, keyword, [], []);

  public static IReadOnlyList<string> BuildGapTopicsFromResearch(
        IReadOnlyList<Finding> gapFindings,
        IReadOnlyList<SerpItem> serpItems,
        string keyword,
        IReadOnlyList<string> targetHeadingTexts,
        IReadOnlyList<string> competitorHeadingTexts)
    {
        var topics = new List<string>();
        var normalizedKeyword = keyword.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            topics.Add(normalizedKeyword);

        foreach (var finding in gapFindings)
            topics.AddRange(ParseGapTopicsFromFinding(finding));

        var targetNormalized = targetHeadingTexts
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeGapHeading)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var heading in competitorHeadingTexts.Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            var text = heading.Trim();
            if (text.Length < 4)
                continue;

            var normalized = NormalizeGapHeading(text);
            if (targetNormalized.Contains(normalized))
                continue;
            if (normalizedKeyword.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
                continue;

            topics.Add(text);
        }

        foreach (var heading in targetHeadingTexts.Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            var text = heading.Trim();
            if (text.Length < 4)
                continue;
            topics.Add(text);
        }

        if (topics.Count <= 1)
        {
            topics.AddRange(
                ExtractRelatedQueries(serpItems)
                    .Where(q => !q.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
                    .Take(5));
        }

        return topics
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToList();
    }

    private static string NormalizeGapHeading(string text) =>
        text.Trim().ToLowerInvariant();

    public static string? FindMatchedPillarAngle(IReadOnlyList<SerpItem> serpItems)
    {
        var paa = serpItems
            .Where(i => i.RelatedQueries.Count > 0)
            .SelectMany(i => i.RelatedQueries.OrderBy(q => q.Sequence))
            .Select(q => q.QueryText)
            .FirstOrDefault(q => !string.IsNullOrWhiteSpace(q));

        return string.IsNullOrWhiteSpace(paa) ? null : paa.Trim();
    }

    public static string BuildWritingInstructions(
        SiteProfileAssemblyWrite siteWrite,
        RunWritingFocusWrite runWrite,
        string keyword)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(siteWrite.BusinessSummary))
            lines.Add($"Business: {siteWrite.BusinessSummary.Trim()}");

        if (!string.IsNullOrWhiteSpace(siteWrite.PrimaryNiche))
            lines.Add($"Site niche: {siteWrite.PrimaryNiche.Trim()}.");

        if (!string.IsNullOrWhiteSpace(siteWrite.NicheDescription))
            lines.Add(siteWrite.NicheDescription.Trim());

        if (siteWrite.NicheTags.Count > 0)
            lines.Add($"Themes: {string.Join(", ", siteWrite.NicheTags.Take(8))}.");

        if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarTopic))
        {
            var pillar = $"Align with topic cluster \"{runWrite.MatchedPillarTopic}\"";
            if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarIntent))
                pillar += $" ({runWrite.MatchedPillarIntent})";
            if (!string.IsNullOrWhiteSpace(runWrite.MatchedPillarAngle))
                pillar += $". Angle: {runWrite.MatchedPillarAngle}";
            lines.Add(pillar + ".");
        }

        if (siteWrite.GeoAnchorNodes.Count > 0)
            lines.Add($"Geo context: {string.Join("; ", siteWrite.GeoAnchorNodes.Take(4))}.");

        if (!string.IsNullOrWhiteSpace(siteWrite.ServiceAreaDescription))
            lines.Add(siteWrite.ServiceAreaDescription);

        if (runWrite.GapTopics.Count > 0)
            lines.Add($"Address content gaps: {string.Join(", ", runWrite.GapTopics)}.");

        if (siteWrite.CompetitorDomains.Count > 0)
            lines.Add($"SERP competitors: {string.Join(", ", siteWrite.CompetitorDomains.Take(5))}.");

        lines.Add($"Target keyword: {keyword.Trim()}.");
        lines.Add(ContentQualityBarInstruction);

        return string.Join(" ", lines).Trim();
    }

    private static IEnumerable<string> ParseGapTopicsFromFinding(Finding finding)
    {
        var topics = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(finding.PayloadJson);
            var root = doc.RootElement;

            if (finding.FindingType == FindingType.StructuredDataGap
                && root.TryGetProperty("missingTypes", out var types)
                && types.ValueKind == JsonValueKind.Array)
            {
                foreach (var type in types.EnumerateArray())
                {
                    var value = TryReadJsonString(type);
                    if (!string.IsNullOrWhiteSpace(value))
                        topics.Add($"Add {value.Trim()} schema markup");
                }
            }

            if (finding.FindingType == FindingType.ContentBlockGap
                && root.TryGetProperty("missingBlockTypes", out var blocks)
                && blocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in blocks.EnumerateArray())
                {
                    var value = TryReadJsonString(block);
                    if (!string.IsNullOrWhiteSpace(value))
                        topics.Add($"Add {value.Trim()} content block");
                }
            }

            if (finding.FindingType == FindingType.HeadingStructureGap
                && root.TryGetProperty("competitorMedianH2ToH6", out var median)
                && root.TryGetProperty("targetH2ToH6", out var target))
            {
                topics.Add(
                    $"Expand H2-H6 structure (target {target.GetInt32()}, competitor median {median.GetInt32()})");
            }
        }
        catch (JsonException)
        {
            // ignore malformed finding payloads
        }

        return topics;
    }

    private static List<string> ExtractRelatedQueries(IReadOnlyList<SerpItem> serpItems) =>
        serpItems
            .Where(i => string.Equals(i.Type, SerpItemTypes.RelatedSearches, StringComparison.OrdinalIgnoreCase)
                || i.RelatedQueries.Count > 0)
            .SelectMany(i => i.RelatedQueries)
            .OrderBy(q => q.Sequence)
            .Select(q => q.QueryText)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<string> ParseGeoNodesFromJsonLd(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return WalkJsonForGeo(doc.RootElement).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> WalkJsonForGeo(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("addressLocality")
                        || property.NameEquals("addressRegion")
                        || property.NameEquals("addressCountry")
                        || property.NameEquals("streetAddress"))
                    {
                        var text = TryReadJsonString(property.Value);
                        if (!string.IsNullOrWhiteSpace(text))
                            yield return text.Trim();
                    }
                    else
                    {
                        foreach (var nested in WalkJsonForGeo(property.Value))
                            yield return nested;
                    }
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in WalkJsonForGeo(item))
                        yield return nested;
                }
                break;
        }
    }

    private static IEnumerable<PageJsonLd> PrioritizeBusinessJsonLd(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        static bool IsBusinessBlock(PageJsonLd block)
        {
            var types = ParseJsonLdTypes(block.RawJson);
            if (types.Count == 0 && !string.IsNullOrWhiteSpace(block.ParsedType))
                types = [block.ParsedType];

            return types.Any(t => PreferredBusinessSchemaTypes.Contains(t, StringComparer.OrdinalIgnoreCase));
        }

        var business = jsonLdBlocks.Where(IsBusinessBlock).ToList();
        return business.Count > 0 ? business : jsonLdBlocks;
    }

    private static List<string> ParseJsonLdTypes(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return ParseTypesElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseTypesElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
                {
                    var preferred = SelectPreferredGraphEntity(element);
                    if (preferred is { } entity && entity.TryGetProperty("@type", out var graphType))
                        return ReadTypeValues(graphType);
                }

                if (element.TryGetProperty("@type", out var typeProp))
                    return ReadTypeValues(typeProp);

                return [];

            case JsonValueKind.Array:
                var bestEntity = SelectBestEntityFromArray(element);
                if (bestEntity is { } best && best.TryGetProperty("@type", out var bestType))
                    return ReadTypeValues(bestType);

                foreach (var item in element.EnumerateArray())
                {
                    var arrayTypes = ParseTypesElement(item);
                    if (arrayTypes.Count > 0)
                        return arrayTypes;
                }

                return [];

            default:
                return [];
        }
    }

    private static JsonElement? SelectPreferredGraphEntity(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        if (root.TryGetProperty("@type", out var rootType) && ScoreBusinessTypes(ReadTypeValues(rootType)) > 0)
            return root;

        if (!root.TryGetProperty("@graph", out var graph) || graph.ValueKind != JsonValueKind.Array)
            return null;

        return SelectBestEntityFromArray(graph);
    }

    private static JsonElement? SelectBestEntityFromArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
            return null;

        JsonElement? best = null;
        var bestScore = 0;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            if (!item.TryGetProperty("@type", out var typeProp))
                continue;

            var score = ScoreBusinessTypes(ReadTypeValues(typeProp));
            if (score > bestScore)
            {
                bestScore = score;
                best = item;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static int ScoreBusinessTypes(IReadOnlyList<string> types)
    {
        var score = 0;
        for (var i = 0; i < PreferredBusinessSchemaTypes.Length; i++)
        {
            if (types.Any(t => string.Equals(t, PreferredBusinessSchemaTypes[i], StringComparison.OrdinalIgnoreCase)))
                score = Math.Max(score, PreferredBusinessSchemaTypes.Length - i);
        }

        return score;
    }

    private static List<string> ReadTypeValues(JsonElement typeProp)
    {
        if (typeProp.ValueKind == JsonValueKind.String)
        {
            var value = typeProp.GetString();
            return string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];
        }

        if (typeProp.ValueKind == JsonValueKind.Array)
        {
            return typeProp.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .ToList();
        }

        return [];
    }

    private static string FormatBusinessTypeLabel(IReadOnlyList<string> types)
    {
        static string Label(string type) =>
            type.EndsWith("Business", StringComparison.OrdinalIgnoreCase)
                ? type[..^8] + " business"
                : HumanizeSchemaToken(type);

        return string.Join(" · ", types.Select(Label).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string HumanizeSchemaToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return token;

        var spaced = string.Concat(token.Select((ch, index) =>
            index > 0 && char.IsUpper(ch) && !char.IsUpper(token[index - 1]) ? " " + ch : ch.ToString()));

        return spaced.Trim();
    }

    private static string? FormatJsonLdAreaServed(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var preferred = SelectPreferredGraphEntity(doc.RootElement);
            if (preferred is { } entity)
            {
                var fromEntity = FormatAreaServedElement(entity);
                if (!string.IsNullOrWhiteSpace(fromEntity))
                    return fromEntity;
            }

            return FormatAreaServedElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FormatAreaServedElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("areaServed") || property.NameEquals("serviceArea"))
                {
                    var formatted = FormatAreaServedValue(property.Value);
                    if (!string.IsNullOrWhiteSpace(formatted))
                        return formatted;
                }

                var nested = FormatAreaServedElement(property.Value);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FormatAreaServedElement(item);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static string? FormatAreaServedValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Array => string.Join(
                ", ",
                value.EnumerateArray()
                    .Select(FormatPlaceName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))),
            JsonValueKind.Object => FormatPlaceName(value),
            _ => null,
        };
    }

    private static string? FormatPlaceName(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.String or JsonValueKind.Number)
            return TryReadJsonString(element);

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var name = element.TryGetProperty("name", out var nameProp) ? TryReadJsonString(nameProp) : null;
        string? region = null;
        if (element.TryGetProperty("containedInPlace", out var regionProp))
            region = FormatPlaceName(regionProp) ?? TryReadJsonString(regionProp);
        else if (element.TryGetProperty("addressRegion", out var stateProp))
            region = TryReadJsonString(stateProp);

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(region))
            return $"{name.Trim()} ({region.Trim()})";

        return !string.IsNullOrWhiteSpace(name) ? name.Trim() : region?.Trim();
    }

    private static IEnumerable<string> ExtractTopicTagsFromJsonLd(IReadOnlyList<PageJsonLd> jsonLdBlocks)
    {
        var tags = new List<string>();
        foreach (var block in PrioritizeBusinessJsonLd(jsonLdBlocks))
        {
            tags.AddRange(ParseJsonLdStringArray(block.RawJson, "knowsAbout"));
            tags.AddRange(ParseJsonLdServiceNames(block.RawJson));
        }

        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ParseJsonLdStringArray(string rawJson, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return FindStringArrayProperty(doc.RootElement, propertyName).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> FindStringArrayProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                                yield return value.Trim();
                        }
                        else if (item.ValueKind == JsonValueKind.Object
                            && item.TryGetProperty("name", out var name)
                            && name.ValueKind == JsonValueKind.String)
                        {
                            var value = name.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                                yield return value.Trim();
                        }
                    }
                }

                foreach (var nested in FindStringArrayProperty(property.Value, propertyName))
                    yield return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in FindStringArrayProperty(item, propertyName))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<string> ParseJsonLdServiceNames(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return FindOfferNames(doc.RootElement).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> FindOfferNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("itemOffered")
                    || property.NameEquals("name")
                    || property.NameEquals("serviceType"))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            yield return value.Trim();
                    }
                    else
                    {
                        foreach (var nested in FindOfferNames(property.Value))
                            yield return nested;
                    }
                }
                else
                {
                    foreach (var nested in FindOfferNames(property.Value))
                        yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in FindOfferNames(item))
                    yield return nested;
            }
        }
    }

    private static string? ParseJsonLdType(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return ParseTypeElement(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ParseTypeElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("@type", out var typeProp))
            {
                var types = ReadTypeValues(typeProp);
                return types.FirstOrDefault();
            }

            if (element.TryGetProperty("type", out var altType))
                return TryReadJsonString(altType);

            if (element.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in graph.EnumerateArray())
                {
                    var graphType = ParseTypeElement(item);
                    if (!string.IsNullOrWhiteSpace(graphType))
                        return graphType;
                }
            }
        }

        return null;
    }

    private static string? ParseJsonLdStringProperty(string rawJson, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var preferred = SelectPreferredGraphEntity(doc.RootElement);
            if (preferred is { } entity)
            {
                var direct = ReadStringPropertyFromObject(entity, propertyName);
                if (!string.IsNullOrWhiteSpace(direct))
                    return direct;
            }

            return FindStringProperty(doc.RootElement, propertyName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadStringPropertyFromObject(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(propertyName))
                return TryReadJsonString(property.Value);
        }

        return null;
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName))
                    return TryReadJsonString(property.Value);

                var nested = FindStringProperty(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindStringProperty(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static string? TryReadJsonString(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString()?.Trim();
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetRawText().Trim();
            case JsonValueKind.Object:
                if (element.TryGetProperty("name", out var name))
                {
                    var fromName = TryReadJsonString(name);
                    if (!string.IsNullOrWhiteSpace(fromName))
                        return fromName;
                }

                if (element.TryGetProperty("@value", out var value))
                {
                    var fromValue = TryReadJsonString(value);
                    if (!string.IsNullOrWhiteSpace(fromValue))
                        return fromValue;
                }

                return null;
            default:
                return null;
        }
    }
}
