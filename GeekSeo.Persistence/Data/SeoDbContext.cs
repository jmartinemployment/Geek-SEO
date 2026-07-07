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
    public DbSet<NicheProfile> NicheProfiles => Set<NicheProfile>();
    public DbSet<NichePillar> NichePillars => Set<NichePillar>();
    public DbSet<NicheSubtopic> NicheSubtopics => Set<NicheSubtopic>();
    public DbSet<NicheCompetitor> NicheCompetitors => Set<NicheCompetitor>();
    public DbSet<NicheEntity> NicheEntities => Set<NicheEntity>();
    public DbSet<NichePillarPage> NichePillarPages => Set<NichePillarPage>();
    public DbSet<NicheTopicCandidate> NicheTopicCandidates => Set<NicheTopicCandidate>();
    public DbSet<NicheProfileStepRun> NicheProfileStepRuns => Set<NicheProfileStepRun>();
    public DbSet<NicheProfileSchemaSignal> NicheProfileSchemaSignals => Set<NicheProfileSchemaSignal>();
    public DbSet<NicheProfileDiscoveredUrl> NicheProfileDiscoveredUrls => Set<NicheProfileDiscoveredUrl>();
    public DbSet<NicheProfileNavigationLink> NicheProfileNavigationLinks => Set<NicheProfileNavigationLink>();
    public DbSet<NicheProfileHeading> NicheProfileHeadings => Set<NicheProfileHeading>();
    public DbSet<NicheProfilePageContentItem> NicheProfilePageContentItems => Set<NicheProfilePageContentItem>();
    public DbSet<NicheProfilePageContentMeta> NicheProfilePageContentMetaRows => Set<NicheProfilePageContentMeta>();
    public DbSet<NicheProfileSitePage> NicheProfileSitePages => Set<NicheProfileSitePage>();
    public DbSet<NicheProfileSitePageLink> NicheProfileSitePageLinks => Set<NicheProfileSitePageLink>();
    public DbSet<NicheProfileUrlPatternTopic> NicheProfileUrlPatternTopics => Set<NicheProfileUrlPatternTopic>();
    public DbSet<NicheProfileSiteCrawlMeta> NicheProfileSiteCrawlMetaRows => Set<NicheProfileSiteCrawlMeta>();
    public DbSet<NicheTopicCandidateEvidence> NicheTopicCandidateEvidenceRows => Set<NicheTopicCandidateEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("geek_seo");
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
