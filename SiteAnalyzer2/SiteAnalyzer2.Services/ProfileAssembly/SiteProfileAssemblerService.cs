using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.ProfileAssembly;

public sealed class SiteProfileAssemblerService(
    ISiteProfileAssemblerRepository repository,
    IRunProgressNotifier progressNotifier,
    IHttpClientFactory httpClientFactory,
    PageExtractionService pageExtractionService)
{
    public async Task AssembleFromHomepageAsync(Guid siteProfileId, CancellationToken ct = default)
    {
        var profile = await repository.GetSiteProfileByIdAsync(siteProfileId, ct)
            ?? throw new InvalidOperationException($"Site profile {siteProfileId} not found.");

        if (!Uri.TryCreate(profile.SiteUrl, UriKind.Absolute, out var homepageUri))
        {
            throw new InvalidOperationException($"Site profile {siteProfileId} has an invalid site URL.");
        }

        var client = httpClientFactory.CreateClient(nameof(SiteProfileAssemblerService));
        client.Timeout = TimeSpan.FromSeconds(45);

        string html;
        try
        {
            using var response = await client.GetAsync(homepageUri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Could not fetch homepage for {profile.SiteUrl} (HTTP {(int)response.StatusCode}).");
            }

            html = await response.Content.ReadAsStringAsync(ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException(
                $"Could not fetch homepage for {profile.SiteUrl}. {ex.Message}",
                ex);
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException($"Homepage for {profile.SiteUrl} returned empty content.");
        }

        var domain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(profile.SiteUrl));
        var extraction = pageExtractionService.Extract(html, homepageUri, domain);
        var homepage = MapHomepageSnapshot(profile.SiteUrl, extraction);
        var displayName = string.IsNullOrWhiteSpace(profile.DisplayName)
            ? domain
            : profile.DisplayName.Trim();
        var siteWrite = SiteProfileAssemblerHelpers.BuildSiteProfileFromHomepage(
            homepage,
            displayName);

        SiteProfileAssemblerHelpers.ValidateHomepageOutput(siteWrite);

        await repository.PersistSiteProfileAsync(siteProfileId, siteWrite, ct);
    }

    public async Task AssembleForRunAsync(Guid runId, CancellationToken ct = default)
    {
        var siteProfileId = await ResolveSiteProfileIdAsync(runId, ct);
        var source = await repository.LoadAssemblySourceAsync(siteProfileId, runId, ct);
        await AssembleFromSourceAsync(source, ct);
    }

    /// <summary>Operator path: persist run focus + SERP-derived site fields without rebuilding homepage niche tags.</summary>
    public async Task AssembleOperatorRunFocusAsync(Guid runId, CancellationToken ct = default)
    {
        var siteProfileId = await ResolveSiteProfileIdAsync(runId, ct);
        var source = await repository.LoadAssemblySourceAsync(siteProfileId, runId, ct);
        await AssembleOperatorFromSourceAsync(source, ct);
    }

    public async Task AssembleAsync(Guid siteProfileId, Guid runId, CancellationToken ct = default)
    {
        var source = await repository.LoadAssemblySourceAsync(siteProfileId, runId, ct);
        await AssembleFromSourceAsync(source, ct);
    }

    private async Task AssembleFromSourceAsync(SiteProfileAssemblySource source, CancellationToken ct)
    {
        ValidateRunSource(source);

        var normalizedRunUrl = TargetSiteUrlNormalizer.Normalize(source.Run.TargetSiteUrl);
        if (!TargetSiteUrlNormalizer.Equals(normalizedRunUrl, source.SiteProfile.SiteUrl))
        {
            throw new InvalidOperationException(
                $"Run {source.Run.Id} target URL does not match site profile {source.SiteProfile.Id}.");
        }

        var headingTexts = source.TargetPages
            .SelectMany(p => p.Headings.Where(h => h.Level <= 2))
            .OrderBy(h => h.Sequence)
            .Select(h => h.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var keyword = source.Run.Keyword.Trim();
        var siteWrite = SiteProfileAssemblerHelpers.BuildSiteProfileFromRunSource(source);
        var baselineRecommendations = source.SiteProfile.WritingRecommendations.Count > 0
            ? (IReadOnlyList<string>)source.SiteProfile.WritingRecommendations
            : siteWrite.WritingRecommendations;
        var siteWriteWithBaseline = new SiteProfileAssemblyWrite
        {
            BusinessType = siteWrite.BusinessType,
            BusinessDescription = siteWrite.BusinessDescription,
            BusinessSummary = siteWrite.BusinessSummary,
            ServiceAreaDescription = siteWrite.ServiceAreaDescription,
            GeoAnchorNodes = siteWrite.GeoAnchorNodes,
            PrimaryNiche = siteWrite.PrimaryNiche,
            NicheDescription = siteWrite.NicheDescription,
            NicheTags = siteWrite.NicheTags,
            CompetitorDomains = siteWrite.CompetitorDomains,
            AuthorityPageUrls = siteWrite.AuthorityPageUrls,
            WritingRecommendations = baselineRecommendations,
            HomepageBusinessSchemaJson = siteWrite.HomepageBusinessSchemaJson
                ?? source.SiteProfile.HomepageBusinessSchemaJson,
        };

        var matchedPillarTopic = SiteProfileAssemblerHelpers.FindMatchedPillarTopic(keyword, headingTexts);
        var matchedPillarIntent = SiteProfileAssemblerHelpers.InferSearchIntent(source.SerpItems, keyword);
        var matchedPillarAngle = SiteProfileAssemblerHelpers.FindMatchedPillarAngle(source.SerpItems);
        var gapTopics = SiteProfileAssemblerHelpers.BuildGapTopics(source.GapFindings, source.SerpItems, keyword);

        var runWrite = new RunWritingFocusWrite
        {
            MatchedPillarTopic = matchedPillarTopic,
            MatchedPillarIntent = matchedPillarIntent,
            MatchedPillarAngle = matchedPillarAngle,
            GapTopics = gapTopics,
            WritingInstructions = SiteProfileAssemblerHelpers.BuildWritingInstructions(
                siteWriteWithBaseline,
                new RunWritingFocusWrite
                {
                    MatchedPillarTopic = matchedPillarTopic,
                    MatchedPillarIntent = matchedPillarIntent,
                    MatchedPillarAngle = matchedPillarAngle,
                    GapTopics = gapTopics,
                },
                keyword),
        };

        var enrichedSiteWrite = new SiteProfileAssemblyWrite
        {
            BusinessType = siteWriteWithBaseline.BusinessType,
            BusinessDescription = siteWriteWithBaseline.BusinessDescription,
            BusinessSummary = siteWriteWithBaseline.BusinessSummary,
            ServiceAreaDescription = siteWriteWithBaseline.ServiceAreaDescription,
            GeoAnchorNodes = siteWriteWithBaseline.GeoAnchorNodes,
            PrimaryNiche = siteWriteWithBaseline.PrimaryNiche,
            NicheDescription = siteWriteWithBaseline.NicheDescription,
            NicheTags = siteWriteWithBaseline.NicheTags,
            CompetitorDomains = siteWriteWithBaseline.CompetitorDomains,
            AuthorityPageUrls = siteWriteWithBaseline.AuthorityPageUrls,
            WritingRecommendations = SiteProfileAssemblerHelpers.BuildRunWritingRecommendations(
                siteWriteWithBaseline,
                runWrite,
                keyword),
            HomepageBusinessSchemaJson = siteWriteWithBaseline.HomepageBusinessSchemaJson,
        };

        ValidateRunOutput(enrichedSiteWrite);

        await repository.PersistAsync(
            source.SiteProfile.Id,
            source.Run.Id,
            enrichedSiteWrite,
            runWrite,
            ct);

        await progressNotifier.NotifySiteProfileUpdated(source.Run.Id, source.SiteProfile.Id, ct);
    }

    private async Task AssembleOperatorFromSourceAsync(SiteProfileAssemblySource source, CancellationToken ct)
    {
        if (source.TargetPages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Run focus assembly requires crawled target-site pages for run {source.Run.Id}.");
        }

        var organicCount = source.SerpItems.Count(i =>
            i.Type == SerpItemTypes.Organic && !i.Ads);
        if (organicCount == 0)
        {
            throw new InvalidOperationException(
                $"Run focus assembly requires organic SERP items for run {source.Run.Id}.");
        }

        var headingTexts = source.TargetPages
            .SelectMany(p => p.Headings.Where(h => h.Level >= 2))
            .OrderBy(h => h.Sequence)
            .Select(h => h.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var competitorHeadingTexts = source.CompetitorHeadingTexts.ToList();
        var keyword = source.Run.Keyword.Trim();
        var siteWrite = SiteProfileAssemblerHelpers.BuildSiteProfileFromRunSource(source);
        var baselineRecommendations = source.SiteProfile.WritingRecommendations.Count > 0
            ? (IReadOnlyList<string>)source.SiteProfile.WritingRecommendations
            : siteWrite.WritingRecommendations;

        var siteWriteWithBaseline = new SiteProfileAssemblyWrite
        {
            BusinessType = source.SiteProfile.BusinessType ?? siteWrite.BusinessType,
            BusinessDescription = source.SiteProfile.BusinessDescription ?? siteWrite.BusinessDescription,
            BusinessSummary = source.SiteProfile.BusinessSummary ?? siteWrite.BusinessSummary,
            ServiceAreaDescription = source.SiteProfile.ServiceAreaDescription ?? siteWrite.ServiceAreaDescription,
            GeoAnchorNodes = source.SiteProfile.GeoAnchorNodes.Count > 0
                ? source.SiteProfile.GeoAnchorNodes
                : siteWrite.GeoAnchorNodes,
            PrimaryNiche = source.SiteProfile.PrimaryNiche ?? siteWrite.PrimaryNiche,
            NicheDescription = siteWrite.NicheDescription,
            NicheTags = source.SiteProfile.NicheTags.Count > 0 ? source.SiteProfile.NicheTags : siteWrite.NicheTags,
            CompetitorDomains = siteWrite.CompetitorDomains,
            AuthorityPageUrls = siteWrite.AuthorityPageUrls,
            WritingRecommendations = baselineRecommendations,
            HomepageBusinessSchemaJson = siteWrite.HomepageBusinessSchemaJson
                ?? source.SiteProfile.HomepageBusinessSchemaJson,
        };

        var matchedPillarTopic = keyword;
        var matchedPillarIntent = SiteProfileAssemblerHelpers.InferSearchIntent(source.SerpItems, keyword);
        var matchedPillarAngle = SiteProfileAssemblerHelpers.FindMatchedPillarAngle(source.SerpItems);
        var gapTopics = SiteProfileAssemblerHelpers.BuildGapTopicsFromResearch(
            source.GapFindings,
            source.SerpItems,
            keyword,
            headingTexts,
            competitorHeadingTexts);

        var runWrite = new RunWritingFocusWrite
        {
            MatchedPillarTopic = matchedPillarTopic,
            MatchedPillarIntent = matchedPillarIntent,
            MatchedPillarAngle = matchedPillarAngle,
            GapTopics = gapTopics,
            WritingInstructions = SiteProfileAssemblerHelpers.BuildWritingInstructions(
                siteWriteWithBaseline,
                new RunWritingFocusWrite
                {
                    MatchedPillarTopic = matchedPillarTopic,
                    MatchedPillarIntent = matchedPillarIntent,
                    MatchedPillarAngle = matchedPillarAngle,
                    GapTopics = gapTopics,
                },
                keyword),
        };

        await repository.PersistAsync(
            source.SiteProfile.Id,
            source.Run.Id,
            siteWriteWithBaseline,
            runWrite,
            ct);

        await progressNotifier.NotifySiteProfileUpdated(source.Run.Id, source.SiteProfile.Id, ct);
    }

    private async Task<Guid> ResolveSiteProfileIdAsync(Guid runId, CancellationToken ct)
    {
        var targetUrl = await repository.GetRunTargetSiteUrlAsync(runId, ct);
        var normalized = TargetSiteUrlNormalizer.Normalize(targetUrl);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new InvalidOperationException(
                $"Run {runId} has no target site URL for site profile assembly.");
        }

        var siteProfileId = await repository.GetSiteProfileIdBySiteUrlAsync(normalized, ct);
        if (siteProfileId is not Guid profileId)
        {
            throw new InvalidOperationException(
                $"No site profile exists for {normalized}. Create the site profile before running analysis.");
        }

        return profileId;
    }

    private static TargetPageSnapshot MapHomepageSnapshot(string siteUrl, PageExtractionResult extraction) =>
        new()
        {
            Page = new Page { Url = siteUrl },
            Headings = extraction.Headings
                .Select(h => new PageHeading
                {
                    Level = h.Level,
                    Text = h.Text,
                    Sequence = h.Sequence,
                })
                .ToList(),
            MetaTags = extraction.MetaTags
                .Select(m => new PageMetaTag
                {
                    NameOrProperty = m.NameOrProperty,
                    Content = m.Content,
                })
                .ToList(),
            JsonLdBlocks = extraction.JsonLdBlocks
                .Select(j => new PageJsonLd
                {
                    RawJson = j.RawJson,
                    ParsedType = j.ParsedType,
                })
                .ToList(),
            InternalLinks = extraction.InternalLinks
                .Select(l => new TargetPageInternalLink
                {
                    AbsoluteUrl = l.AbsoluteUrl,
                    AnchorText = l.AnchorText,
                })
                .ToList(),
        };

    private static void ValidateRunSource(SiteProfileAssemblySource source)
    {
        if (source.TargetPages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Site profile assembly requires crawled target-site pages for run {source.Run.Id}.");
        }

        var organicCount = source.SerpItems.Count(i =>
            i.Type == SerpItemTypes.Organic && !i.Ads);

        if (organicCount == 0)
        {
            throw new InvalidOperationException(
                $"Site profile assembly requires organic SERP items for run {source.Run.Id}.");
        }
    }

    private static void ValidateRunOutput(SiteProfileAssemblyWrite siteWrite)
    {
        if (string.IsNullOrWhiteSpace(siteWrite.BusinessType))
        {
            throw new InvalidOperationException(
                "Site profile assembly could not derive BusinessType from crawled JSON-LD.");
        }

        if (siteWrite.CompetitorDomains.Count == 0)
        {
            throw new InvalidOperationException(
                "Site profile assembly could not derive CompetitorDomains from SERP data.");
        }

        if (siteWrite.NicheTags.Count == 0)
        {
            throw new InvalidOperationException(
                "Site profile assembly could not derive NicheTags from site keywords or SERP data.");
        }
    }
}
