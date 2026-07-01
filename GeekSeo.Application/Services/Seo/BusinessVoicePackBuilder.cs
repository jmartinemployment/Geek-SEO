using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class BusinessVoicePackBuilder
{
    private static readonly string[] ImplementationSignals =
    [
        "consult",
        "integrat",
        "implement",
        "deploy",
        "automation",
        "chatbot",
        "development",
        "professional service",
        "local business",
    ];

    private static readonly (string Id, string[] Tokens)[] CapabilityCatalog =
    [
        ("AI chatbots", ["chatbot", "chat bot", "conversational ai"]),
        ("process automation", ["process automation", "workflow automation", "zapier"]),
        ("custom React apps", ["react", "angular", "next.js", "nextjs"]),
        ("Node.js APIs", ["node.js", "nodejs", "node js"]),
        ("Postgres analytics", ["postgres", "postgresql", "sql database"]),
        ("data analytics", ["data analytics", "analytics pipeline", "dashboard"]),
        ("WordPress sites", ["wordpress"]),
        ("Microsoft .NET", ["c#", "microsoft c#", ".net"]),
    ];

    private static readonly string[] DefaultToolExamples =
    [
        "Shopify",
        "HubSpot",
        "QuickBooks",
        "WordPress",
        "Postgres",
        "React",
        "Zapier",
    ];

    public static BusinessVoicePack Build(WritingResearchContext research)
    {
        var focus = research.SiteFocus;
        var corpus = BuildCorpus(research, focus);
        var isImplementation = LooksLikeImplementationConsultancy(corpus);
        var capabilities = DetectCapabilities(corpus);
        var geo = FormatGeoLabel(focus, research.SearchLocation);
        var hasLocalMarket = !string.IsNullOrWhiteSpace(geo);
        var siteName = focus?.SiteName?.Trim() ?? string.Empty;
        var siteUrl = focus?.SiteUrl?.Trim() ?? research.SourceUrl.Trim();
        var recommendations = CollectWritingRecommendations(focus, corpus);
        var toolExamples = PickToolExamples(corpus, research.RecommendedTerms.Select(t => t.Term));

        var enabled = focus is not null
            || !string.IsNullOrWhiteSpace(research.BusinessContext)
            || isImplementation;

        return new BusinessVoicePack
        {
            Enabled = enabled,
            Keyword = research.DerivedKeyword,
            SiteName = siteName,
            SiteUrl = siteUrl,
            GeoLabel = geo,
            IsImplementationConsultancy = isImplementation,
            DeclaredCapabilities = capabilities,
            SuggestedToolExamples = toolExamples,
            WritingRecommendations = recommendations,
            MinimumConcreteExamples = isImplementation ? 3 : 2,
            RequiresTraditionalVsAiContrast = isImplementation,
            RequiresPerSectionContrast = isImplementation,
            RequiresCapabilityBridge = capabilities.Count > 0,
            RequiresLocalMarketExamples = isImplementation && hasLocalMarket,
            MinimumLocalMarketExamples = 2,
            CtaParagraphHtml = BuildCtaParagraphHtml(research.DerivedKeyword, siteName, siteUrl, geo),
            DataQualityPhaseLabel = WritingMethodologySpec.FourPhase.PhaseDefinitions
                .First(p => p.Id == "data-quality-assessment")
                .Label,
        };
    }

    private static string BuildCorpus(WritingResearchContext research, SiteWritingFocus? focus)
    {
        var parts = new List<string>
        {
            research.BusinessContext,
            focus?.BusinessSummary ?? string.Empty,
            focus?.PrimaryNiche ?? string.Empty,
            focus?.NicheDescription ?? string.Empty,
            focus?.ServiceAreaDescription ?? string.Empty,
            focus?.WritingInstructions ?? string.Empty,
        };

        if (focus is not null)
        {
            parts.AddRange(focus.NicheTags);
        }

        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static bool LooksLikeImplementationConsultancy(string corpus)
    {
        if (string.IsNullOrWhiteSpace(corpus))
            return false;

        var lower = corpus.ToLowerInvariant();
        return ImplementationSignals.Any(signal => lower.Contains(signal, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> DetectCapabilities(string corpus)
    {
        if (string.IsNullOrWhiteSpace(corpus))
            return [];

        var lower = corpus.ToLowerInvariant();
        return CapabilityCatalog
            .Where(cap => cap.Tokens.Any(token => lower.Contains(token, StringComparison.Ordinal)))
            .Select(cap => cap.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static IReadOnlyList<string> PickToolExamples(string corpus, IEnumerable<string> recommendedTerms)
    {
        var lower = corpus.ToLowerInvariant();
        var picks = BusinessVoiceValidator.KnownToolTokens
            .Where(tool => lower.Contains(tool, StringComparison.OrdinalIgnoreCase)
                || recommendedTerms.Any(term => term.Contains(tool, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        foreach (var fallback in DefaultToolExamples)
        {
            if (picks.Count >= 4)
                break;

            if (!picks.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                picks.Add(fallback);
        }

        return picks;
    }

    private static IReadOnlyList<string> CollectWritingRecommendations(SiteWritingFocus? focus, string corpus)
    {
        var recommendations = new List<string>();

        if (!string.IsNullOrWhiteSpace(focus?.WritingInstructions))
            recommendations.Add(focus.WritingInstructions.Trim());

        if (LooksLikeImplementationConsultancy(corpus))
        {
            recommendations.Add(
                "Voice: implementation-first — show named tools, live-data scenarios, and how you deploy chatbots, dashboards, or integrations for SMB clients.");

            if (!string.IsNullOrWhiteSpace(focus?.ServiceAreaDescription)
                || focus?.GeoAnchorNodes.Count > 0)
            {
                recommendations.Add(
                    "Do not write a national category explainer — position as the shop that maps and builds for local SMB buyers who ask \"what would you build for me?\"");
            }
        }

        return recommendations
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string FormatGeoLabel(SiteWritingFocus? focus, string searchLocation)
    {
        if (!string.IsNullOrWhiteSpace(focus?.ServiceAreaDescription))
            return focus.ServiceAreaDescription.Trim();

        if (focus?.GeoAnchorNodes.Count > 0)
            return string.Join("; ", focus.GeoAnchorNodes.Take(3));

        return string.IsNullOrWhiteSpace(searchLocation) || searchLocation.Equals("United States", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : searchLocation.Trim();
    }

    internal static string BuildCtaParagraphHtml(string keyword, string siteName, string siteUrl, string geoLabel)
    {
        var topic = string.IsNullOrWhiteSpace(keyword) ? "this capability" : keyword.Trim();
        var who = string.IsNullOrWhiteSpace(siteName) ? "our team" : siteName.Trim();
        var geoSuffix = string.IsNullOrWhiteSpace(geoLabel)
            ? string.Empty
            : $" Serving {geoLabel.Trim()}.";

        var href = ResolveCtaHref(siteUrl);
        var link = string.IsNullOrWhiteSpace(href)
            ? "Book a free strategy call"
            : $"<a href=\"{href}\">Book a free strategy call</a>";

        return
            $"<p><strong>Want to see what {topic} looks like for your specific tech stack?</strong> " +
            $"Let's map your touchpoints on a free strategy call with {who}.{geoSuffix} {link}.</p>";
    }

    private static string ResolveCtaHref(string siteUrl)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
            return string.Empty;

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri))
            return string.Empty;

        var baseUrl = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return $"{baseUrl}/contact";
    }
}
