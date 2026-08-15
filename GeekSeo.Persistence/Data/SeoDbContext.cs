using GeekSeo.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeekSeo.Persistence.Data;

public partial class SeoDbContext : DbContext
{
    public SeoDbContext(DbContextOptions<SeoDbContext> options)
        : base(options)
    {
    }

    public DbSet<SeoProject> Projects => Set<SeoProject>();
    public DbSet<SeoContentDocument> ContentDocuments => Set<SeoContentDocument>();
    public DbSet<SeoKeywordCluster> KeywordClusters => Set<SeoKeywordCluster>();
    public DbSet<SeoKeyword> Keywords => Set<SeoKeyword>();
    public DbSet<SeoSerpResult> SerpResults => Set<SeoSerpResult>();
    public DbSet<SeoCompetitorPage> CompetitorPages => Set<SeoCompetitorPage>();
    public DbSet<SeoPageAudit> PageAudits => Set<SeoPageAudit>();
    public DbSet<SeoSiteAudit> SiteAudits => Set<SeoSiteAudit>();
    public DbSet<SeoSiteAuditPage> SiteAuditPages => Set<SeoSiteAuditPage>();
    public DbSet<SeoRankTracking> RankTracking => Set<SeoRankTracking>();
    public DbSet<SeoTrackedKeyword> TrackedKeywords => Set<SeoTrackedKeyword>();
    public DbSet<SeoGscConnection> GscConnections => Set<SeoGscConnection>();
    public DbSet<SeoGtmAccountConnection> GtmAccountConnections => Set<SeoGtmAccountConnection>();
    public DbSet<SeoSubscription> Subscriptions => Set<SeoSubscription>();
    public DbSet<SeoReport> Reports => Set<SeoReport>();
    public DbSet<SeoAlert> Alerts => Set<SeoAlert>();
    public DbSet<SeoUsageCounter> UsageCounters => Set<SeoUsageCounter>();
    public DbSet<SeoBackgroundJob> BackgroundJobs => Set<SeoBackgroundJob>();
    public DbSet<SeoWordPressConnection> WordPressConnections => Set<SeoWordPressConnection>();
    public DbSet<SeoPublishedPage> PublishedPages => Set<SeoPublishedPage>();
    public DbSet<SeoContentPerformanceSnapshot> ContentPerformanceSnapshots => Set<SeoContentPerformanceSnapshot>();
    public DbSet<SeoTopicalMap> TopicalMaps => Set<SeoTopicalMap>();
    public DbSet<SeoSitePageInventory> SitePageInventory => Set<SeoSitePageInventory>();
    public DbSet<SeoBrandVoice> BrandVoices => Set<SeoBrandVoice>();
    public DbSet<SeoBulkJob> BulkJobs => Set<SeoBulkJob>();
    public DbSet<SeoPlagiarismCheck> PlagiarismChecks => Set<SeoPlagiarismCheck>();
    public DbSet<SeoGa4Connection> Ga4Connections => Set<SeoGa4Connection>();
    public DbSet<SeoGeoTrackingQuery> GeoTrackingQueries => Set<SeoGeoTrackingQuery>();
    public DbSet<SeoGeoMentionSnapshot> GeoMentionSnapshots => Set<SeoGeoMentionSnapshot>();
    public DbSet<SeoCannibalizationIssue> CannibalizationIssues => Set<SeoCannibalizationIssue>();
    public DbSet<SeoApiKey> ApiKeys => Set<SeoApiKey>();
    public DbSet<SeoSerpDeepCache> SerpDeepCache => Set<SeoSerpDeepCache>();
    public DbSet<SeoKeywordVendorSnapshot> KeywordVendorSnapshots => Set<SeoKeywordVendorSnapshot>();
    public DbSet<SeoContentGuardPolicy> ContentGuardPolicies => Set<SeoContentGuardPolicy>();
    public DbSet<SeoContentGuardRun> ContentGuardRuns => Set<SeoContentGuardRun>();
    public DbSet<SeoOrganization> Organizations => Set<SeoOrganization>();
    public DbSet<SeoOrganizationMember> OrganizationMembers => Set<SeoOrganizationMember>();
    public DbSet<SiteAnalysisProfile> SiteAnalysisProfiles => Set<SiteAnalysisProfile>();
    public DbSet<SiteAnalysisPillar> SiteAnalysisPillars => Set<SiteAnalysisPillar>();
    public DbSet<SiteAnalysisSubtopic> SiteAnalysisSubtopics => Set<SiteAnalysisSubtopic>();
    public DbSet<SiteAnalysisCompetitor> SiteAnalysisCompetitors => Set<SiteAnalysisCompetitor>();
    public DbSet<SiteAnalysisEntity> SiteAnalysisEntities => Set<SiteAnalysisEntity>();
    public DbSet<SiteAnalysisPillarPage> SiteAnalysisPillarPages => Set<SiteAnalysisPillarPage>();
    public DbSet<SiteAnalysisTopicCandidate> SiteAnalysisTopicCandidates => Set<SiteAnalysisTopicCandidate>();
    public DbSet<SiteAnalysisProfileStepRun> SiteAnalysisProfileStepRuns => Set<SiteAnalysisProfileStepRun>();
    public DbSet<SiteAnalysisProfileSchemaSignal> SiteAnalysisProfileSchemaSignals => Set<SiteAnalysisProfileSchemaSignal>();
    public DbSet<SiteAnalysisProfileDiscoveredUrl> SiteAnalysisProfileDiscoveredUrls => Set<SiteAnalysisProfileDiscoveredUrl>();
    public DbSet<SiteAnalysisProfileNavigationLink> SiteAnalysisProfileNavigationLinks => Set<SiteAnalysisProfileNavigationLink>();
    public DbSet<SiteAnalysisProfileHeading> SiteAnalysisProfileHeadings => Set<SiteAnalysisProfileHeading>();
    public DbSet<SiteAnalysisPageSectionTree> SiteAnalysisPageSectionTrees => Set<SiteAnalysisPageSectionTree>();
    public DbSet<SiteAnalysisProfilePageContentItem> SiteAnalysisProfilePageContentItems => Set<SiteAnalysisProfilePageContentItem>();
    public DbSet<SiteAnalysisProfilePageContentMeta> SiteAnalysisProfilePageContentMetaRows => Set<SiteAnalysisProfilePageContentMeta>();
    public DbSet<SiteAnalysisProfileSitePage> SiteAnalysisProfileSitePages => Set<SiteAnalysisProfileSitePage>();
    public DbSet<SiteAnalysisProfileSitePageLink> SiteAnalysisProfileSitePageLinks => Set<SiteAnalysisProfileSitePageLink>();
    public DbSet<SiteAnalysisProfileUrlPatternTopic> SiteAnalysisProfileUrlPatternTopics => Set<SiteAnalysisProfileUrlPatternTopic>();
    public DbSet<SiteAnalysisProfileSiteCrawlMeta> SiteAnalysisProfileSiteCrawlMetaRows => Set<SiteAnalysisProfileSiteCrawlMeta>();
    public DbSet<SiteAnalysisTopicCandidateEvidence> SiteAnalysisTopicCandidateEvidenceRows => Set<SiteAnalysisTopicCandidateEvidence>();
    public DbSet<SeoUrlResearch> UrlResearch => Set<SeoUrlResearch>();
    public DbSet<SeoUrlResearchOrganic> UrlResearchOrganic => Set<SeoUrlResearchOrganic>();
    public DbSet<SeoUrlResearchPaa> UrlResearchPaa => Set<SeoUrlResearchPaa>();
    public DbSet<SeoUrlResearchPasf> UrlResearchPasf => Set<SeoUrlResearchPasf>();
    public DbSet<SeoUrlResearchCompetitor> UrlResearchCompetitors => Set<SeoUrlResearchCompetitor>();
    public DbSet<SeoUrlResearchCompetitorHeading> UrlResearchCompetitorHeadings => Set<SeoUrlResearchCompetitorHeading>();
    public DbSet<SeoUrlResearchSourceHeading> UrlResearchSourceHeadings => Set<SeoUrlResearchSourceHeading>();
    public DbSet<SeoUrlResearchTerm> UrlResearchTerms => Set<SeoUrlResearchTerm>();
    public DbSet<SeoUrlResearchClosingFaq> UrlResearchClosingFaqs => Set<SeoUrlResearchClosingFaq>();
    public DbSet<SeoUrlResearchSectionHint> UrlResearchSectionHints => Set<SeoUrlResearchSectionHint>();
    public DbSet<SeoSiteResearch> SiteResearch => Set<SeoSiteResearch>();
    public DbSet<SeoSiteResearchPage> SiteResearchPages => Set<SeoSiteResearchPage>();
    public DbSet<SeoSiteAnalyzerStepRun> SiteAnalyzerStepRuns => Set<SeoSiteAnalyzerStepRun>();
    public DbSet<SiteAnalysisProfileExtractedTool> ExtractedTools => Set<SiteAnalysisProfileExtractedTool>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("geek_seo");
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
