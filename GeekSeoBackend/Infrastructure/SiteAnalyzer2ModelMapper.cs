using GeekSeo.Application.Models.Seo;
using SiteAnalyzer2.Services.Integrations;

namespace GeekSeoBackend.Infrastructure;

internal static class SiteAnalyzer2ModelMapper
{
    public static AnalysisRunSummary ToSummary(AnalysisRunSummaryDto dto) => new()
    {
        Id = dto.Id,
        ProjectId = dto.ProjectId,
        Keyword = dto.Keyword,
        TargetSiteUrl = dto.TargetSiteUrl,
        Status = dto.Status,
        SerpSeResultsCount = dto.SerpSeResultsCount,
        OrganicResultCount = dto.OrganicResultCount,
        CreatedAt = dto.CreatedAt,
        ContentWritingReady = dto.ContentWritingReady,
    };

    public static ContentWriterSerpExport ToExport(ContentWriterSerpExportDto dto) => new()
    {
        BundleVersion = dto.BundleVersion,
        CapturedAt = dto.CapturedAt,
        RunId = dto.RunId,
        ProjectId = dto.ProjectId,
        GeekSeoProjectId = dto.ProjectId,
        Keyword = dto.Keyword,
        TargetSiteUrl = dto.TargetSiteUrl,
        Status = dto.Status,
        SerpSeResultsCount = dto.SerpSeResultsCount,
        SerpCapturedAt = dto.SerpCapturedAt,
        CompetitorCrawlStatus = dto.CompetitorCrawlStatus,
        CompetitorCrawlFinishedAt = dto.CompetitorCrawlFinishedAt,
        MatchedPillarTopic = dto.MatchedPillarTopic,
        MatchedPillarIntent = dto.MatchedPillarIntent,
        MatchedPillarAngle = dto.MatchedPillarAngle,
        GapTopics = dto.GapTopics,
        WritingInstructions = dto.WritingInstructions,
        WritingRecommendations = dto.WritingRecommendations,
        Serp = dto.Serp.Select(ToSerpItem).ToList(),
        SourceHeadings = dto.SourceHeadings.Select(ToHeading).ToList(),
        Competitors = dto.Competitors.Select(ToCompetitor).ToList(),
        Benchmarks = new ContentWriterExportBenchmarks
        {
            MedianH2CountTop5 = dto.Benchmarks.MedianH2CountTop5,
            MedianWordCountTop5 = dto.Benchmarks.MedianWordCountTop5,
            CompetitorDomainCount = dto.Benchmarks.CompetitorDomainCount,
            CompetitorPageCount = dto.Benchmarks.CompetitorPageCount,
        },
        CitationCandidates = dto.CitationCandidates.Select(c => new ContentWriterCitationCandidate
        {
            Url = c.Url,
            Title = c.Title,
            Domain = c.Domain,
            Source = c.Source,
        }).ToList(),
        ResearchMode = dto.ResearchMode,
        TopicSlug = dto.TopicSlug,
        ManualResearchLanes = dto.ManualResearchLanes.Select(l => new ContentWriterManualResearchLane
        {
            Lane = l.Lane,
            Label = l.Label,
            OrganicCount = l.OrganicCount,
            PaaCount = l.PaaCount,
            OrganicResults = l.OrganicResults.Select(ToSerpItem).ToList(),
            PaaQuestions = l.PaaQuestions.ToList(),
        }).ToList(),
    };

    public static ContentWriterSiteBundle ToSiteBundle(ContentWriterSiteBundleDto dto) => new()
    {
        BundleVersion = dto.BundleVersion,
        CapturedAt = dto.CapturedAt,
        SiteProfileId = dto.SiteProfileId,
        GeekSeoProjectId = dto.GeekSeoProjectId,
        SiteUrl = dto.SiteUrl,
        DisplayName = dto.DisplayName,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        BusinessProfileAt = dto.BusinessProfileAt,
        LastRunAt = dto.LastRunAt,
        BusinessType = dto.BusinessType,
        BusinessDescription = dto.BusinessDescription,
        BusinessSummary = dto.BusinessSummary,
        GeneratedSchemaJson = dto.GeneratedSchemaJson,
        PrimaryNiche = dto.PrimaryNiche,
        NicheDescription = dto.NicheDescription,
        NicheTags = dto.NicheTags,
        GeoAnchorNodes = dto.GeoAnchorNodes,
        ServiceAreaDescription = dto.ServiceAreaDescription,
        CompetitorDomains = dto.CompetitorDomains,
        AuthorityPageUrls = dto.AuthorityPageUrls,
        WritingRecommendations = dto.WritingRecommendations,
        RecommendedHomepageJsonLd = dto.RecommendedHomepageJsonLd.Select(s => new RecommendedJsonLdSnippet
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Json = s.Json,
            ScriptTag = s.ScriptTag,
        }).ToList(),
    };

    public static SiteAnalyzer2SiteProfileExport ToSiteProfileExport(ContentWriterSiteBundleDto dto) => new()
    {
        Id = dto.SiteProfileId,
        SiteUrl = dto.SiteUrl,
        DisplayName = dto.DisplayName,
        GeekSeoProjectId = dto.GeekSeoProjectId,
        PrimaryNiche = dto.PrimaryNiche,
        NicheDescription = dto.NicheDescription,
        NicheTags = dto.NicheTags,
        BusinessSummary = dto.BusinessSummary,
        GeoAnchorNodes = dto.GeoAnchorNodes,
        ServiceAreaDescription = dto.ServiceAreaDescription,
        CompetitorDomains = dto.CompetitorDomains,
        AuthorityPageUrls = dto.AuthorityPageUrls,
        WritingRecommendations = dto.WritingRecommendations,
        UpdatedAt = dto.UpdatedAt,
    };

    private static ContentWriterSerpItem ToSerpItem(ContentWriterSerpItemDto item) => new()
    {
        Position = item.Position,
        Type = item.Type,
        Title = item.Title,
        Url = item.Url,
        Domain = item.Domain,
        Snippet = item.Snippet,
        SiteName = item.SiteName,
        RelatedQuestions = item.RelatedQuestions,
    };

    private static ContentWriterHeading ToHeading(ContentWriterHeadingDto heading) => new()
    {
        Level = heading.Level,
        Text = heading.Text,
        Sequence = heading.Sequence,
    };

    private static ContentWriterCompetitorExport ToCompetitor(ContentWriterCompetitorExportDto c) => new()
    {
        Domain = c.Domain,
        Url = c.Url,
        SeedRankAbsolute = c.SeedRankAbsolute,
        PagesCrawledOnDomain = c.PagesCrawledOnDomain,
        Headings = c.Headings.Select(ToHeading).ToList(),
        WordCountEstimate = c.WordCountEstimate,
        WordCountSource = c.WordCountSource,
        SchemaTypes = c.SchemaTypes,
        HasFaqSchema = c.HasFaqSchema,
    };
}
