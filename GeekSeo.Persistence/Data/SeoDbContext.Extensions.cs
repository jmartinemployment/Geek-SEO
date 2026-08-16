using GeekSeo.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeekSeo.Persistence.Data;

public partial class SeoDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SeoProject>(e =>
        {
            e.ToTable("seo_projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<SeoContentDocument>(e =>
        {
            e.ToTable("seo_content_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Project).WithMany(p => p.ContentDocuments).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UrlResearch).WithMany().HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.DocumentKind).HasMaxLength(32).HasDefaultValue("standalone");
            e.Property(x => x.PublishSlug).HasMaxLength(200);
            e.Property(x => x.SpokeSourceType).HasMaxLength(32);
            e.HasOne(x => x.ParentDocument)
                .WithMany(x => x.ChildDocuments)
                .HasForeignKey(x => x.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.UrlResearchId);
            e.HasIndex(x => x.ParentDocumentId);
            e.HasIndex(x => new { x.ProjectId, x.DocumentKind });
            e.HasIndex(x => new { x.ProjectId, x.PublishSlug })
                .IsUnique()
                .HasFilter("\"PublishSlug\" IS NOT NULL");
        });

        modelBuilder.Entity<SeoKeywordCluster>(e =>
        {
            e.ToTable("seo_keyword_clusters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<SeoKeyword>(e =>
        {
            e.ToTable("seo_keywords");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<SeoSerpResult>(e =>
        {
            e.ToTable("seo_serp_results");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.Keyword, x.Location, x.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<SeoCompetitorPage>(e =>
        {
            e.ToTable("seo_competitor_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SerpResult).WithMany(s => s.CompetitorPages).HasForeignKey(x => x.SerpResultId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SerpResultId, x.Url }).IsUnique();
        });

        modelBuilder.Entity<SeoPageAudit>(e => { e.ToTable("seo_page_audits"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoSiteAudit>(e => { e.ToTable("seo_site_audits"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoSiteAuditPage>(e =>
        {
            e.ToTable("seo_site_audit_pages");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.SiteAudit).WithMany(a => a.Pages).HasForeignKey(x => x.SiteAuditId);
        });

        modelBuilder.Entity<SeoRankTracking>(e =>
        {
            e.ToTable("seo_rank_tracking");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.Keyword, x.Date }).IsUnique();
        });
        modelBuilder.Entity<SeoTrackedKeyword>(e =>
        {
            e.ToTable("seo_tracked_keywords");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.Keyword }).IsUnique();
        });
        modelBuilder.Entity<SeoGscConnection>(e => { e.ToTable("seo_gsc_connections"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProjectId).IsUnique(); });
        modelBuilder.Entity<SeoGtmAccountConnection>(e =>
        {
            e.ToTable("seo_gtm_account_connections");
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountKey).HasMaxLength(128);
            e.HasIndex(x => new { x.UserId, x.AccountKey }).IsUnique();
        });
        modelBuilder.Entity<SeoSubscription>(e => { e.ToTable("seo_subscriptions"); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); });
        modelBuilder.Entity<SeoReport>(e => { e.ToTable("seo_reports"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoAlert>(e => { e.ToTable("seo_alerts"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoUsageCounter>(e =>
        {
            e.ToTable("seo_usage_counters");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.PeriodStart, x.Feature }).IsUnique();
        });

        modelBuilder.Entity<SeoBackgroundJob>(e =>
        {
            e.ToTable("seo_background_jobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.UserId, x.Status });
        });

        modelBuilder.Entity<SeoWordPressConnection>(e => { e.ToTable("seo_wordpress_connections"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProjectId).IsUnique(); });
        modelBuilder.Entity<SeoPublishedPage>(e => { e.ToTable("seo_published_pages"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProjectId, x.Url }).IsUnique(); });
        modelBuilder.Entity<SeoContentPerformanceSnapshot>(e =>
        {
            e.ToTable("seo_content_performance_snapshots");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.PublishedPage).WithMany().HasForeignKey(x => x.PublishedPageId);
            e.HasIndex(x => new { x.PublishedPageId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<SeoTopicalMap>(e =>
        {
            e.ToTable("seo_topical_maps");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId).IsUnique();
        });
        modelBuilder.Entity<SeoSitePageInventory>(e => { e.ToTable("seo_site_page_inventory"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ProjectId, x.Url }).IsUnique(); });
        modelBuilder.Entity<SeoBrandVoice>(e => { e.ToTable("seo_brand_voices"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoBulkJob>(e => { e.ToTable("seo_bulk_jobs"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoPlagiarismCheck>(e => { e.ToTable("seo_plagiarism_checks"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoGa4Connection>(e => { e.ToTable("seo_ga4_connections"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProjectId).IsUnique(); });
        modelBuilder.Entity<SeoGeoTrackingQuery>(e => { e.ToTable("seo_geo_tracking_queries"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoGeoMentionSnapshot>(e =>
        {
            e.ToTable("seo_geo_mention_snapshots");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Query).WithMany(q => q.Snapshots).HasForeignKey(x => x.QueryId);
        });

        modelBuilder.Entity<SeoCannibalizationIssue>(e => { e.ToTable("seo_cannibalization_issues"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoApiKey>(e => { e.ToTable("seo_api_keys"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SeoSerpDeepCache>(e =>
        {
            e.ToTable("seo_serp_deep_cache");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Keyword, x.Location, x.ResultCount }).IsUnique();
        });

        modelBuilder.Entity<SeoKeywordVendorSnapshot>(e =>
        {
            e.ToTable("seo_keyword_vendor_snapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SeedKeyword, x.Location, x.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<SeoContentGuardPolicy>(e =>
        {
            e.ToTable("seo_content_guard_policies");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId).IsUnique();
        });

        modelBuilder.Entity<SeoContentGuardRun>(e =>
        {
            e.ToTable("seo_content_guard_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProjectId, x.Status });
        });

        modelBuilder.Entity<SeoOrganization>(e => { e.ToTable("seo_organizations"); e.HasKey(x => x.Id); e.HasIndex(x => x.Slug).IsUnique(); });
        modelBuilder.Entity<SeoOrganizationMember>(e =>
        {
            e.ToTable("seo_organization_members");
            e.HasKey(x => new { x.OrgId, x.UserId });
            e.HasOne(x => x.Organization).WithMany(o => o.Members).HasForeignKey(x => x.OrgId);
        });

        modelBuilder.Entity<SeoProject>()
            .HasOne<SeoOrganization>()
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SiteAnalysisProfile>(e =>
        {
            e.ToTable("site_analysis_profiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.FocusTags).HasColumnType("text[]");
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Domain);
            e.Property(x => x.AnalysisStepLog)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb");
            e.Property(x => x.FusionSnapshot).HasColumnType("jsonb");
            e.Property(x => x.ScanChangeScore).HasPrecision(5, 4);
        });

        modelBuilder.Entity<SiteAnalysisProfileStepRun>(e =>
        {
            e.ToTable("site_analysis_profile_step_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.StepRuns).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.StepNumber }).IsUnique();
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.StepSlug }).IsUnique();
        });

        modelBuilder.Entity<SiteAnalysisProfileSchemaSignal>(e =>
        {
            e.ToTable("site_analysis_profile_schema_signals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.SchemaSignals).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisProfileDiscoveredUrl>(e =>
        {
            e.ToTable("site_analysis_profile_discovered_urls");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.DiscoveredUrls).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.Url, x.SourceType }).IsUnique();
        });

        modelBuilder.Entity<SiteAnalysisProfileNavigationLink>(e =>
        {
            e.ToTable("site_analysis_profile_navigation_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.NavigationLinks).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisProfileHeading>(e =>
        {
            e.ToTable("site_analysis_profile_headings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.Headings).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisPageSectionTree>(e =>
        {
            e.ToTable("site_analysis_page_section_trees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany().HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisProfilePageContentItem>(e =>
        {
            e.ToTable("site_analysis_profile_page_content_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.PageContentItems).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisProfilePageContentMeta>(e =>
        {
            e.ToTable("site_analysis_profile_page_content_meta");
            e.HasKey(x => x.SiteAnalysisProfileId);
            e.HasOne(x => x.SiteAnalysisProfile).WithOne(p => p.PageContentMeta).HasForeignKey<SiteAnalysisProfilePageContentMeta>(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteAnalysisProfileSitePage>(e =>
        {
            e.ToTable("site_analysis_profile_site_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.SitePages).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.Url }).IsUnique();
            e.Property(x => x.ContextJson)
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");
        });

        modelBuilder.Entity<SiteAnalysisProfileSitePageLink>(e =>
        {
            e.ToTable("site_analysis_profile_site_page_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.SitePageLinks).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisProfileUrlPatternTopic>(e =>
        {
            e.ToTable("site_analysis_profile_url_pattern_topics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.UrlPatternTopics).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.Slug }).IsUnique();
        });

        modelBuilder.Entity<SiteAnalysisProfileSiteCrawlMeta>(e =>
        {
            e.ToTable("site_analysis_profile_site_crawl_meta");
            e.HasKey(x => x.SiteAnalysisProfileId);
            e.HasOne(x => x.SiteAnalysisProfile).WithOne(p => p.SiteCrawlMeta).HasForeignKey<SiteAnalysisProfileSiteCrawlMeta>(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiteAnalysisTopicCandidate>(e =>
        {
            e.ToTable("site_analysis_topic_candidates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.EvidenceJson).HasColumnType("jsonb");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.TopicCandidates).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.Slug }).IsUnique();
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.IsSelected });
        });

        modelBuilder.Entity<SiteAnalysisTopicCandidateEvidence>(e =>
        {
            e.ToTable("site_analysis_topic_candidate_evidence");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.TopicCandidate).WithMany(c => c.EvidenceRows).HasForeignKey(x => x.TopicCandidateId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TopicCandidateId);
        });

        modelBuilder.Entity<SiteAnalysisPillar>(e =>
        {
            e.ToTable("site_analysis_pillars");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.Pillars).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => x.CoverageStatus);
        });

        modelBuilder.Entity<SiteAnalysisSubtopic>(e =>
        {
            e.ToTable("site_analysis_subtopics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Pillar).WithMany(p => p.Subtopics).HasForeignKey(x => x.PillarId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.PillarId);
            e.HasIndex(x => x.IsQuickWin);
        });

        modelBuilder.Entity<SeoUrlResearch>(e =>
        {
            e.ToTable("seo_url_research");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.ProjectId, x.Status });
            e.HasIndex(x => x.SourceUrl);
        });

        modelBuilder.Entity<SeoUrlResearchOrganic>(e =>
        {
            e.ToTable("seo_url_research_organic");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.OrganicResults).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchPaa>(e =>
        {
            e.ToTable("seo_url_research_paa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.PeopleAlsoAsk).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchPasf>(e =>
        {
            e.ToTable("seo_url_research_pasf");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.RelatedSearches).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchCompetitor>(e =>
        {
            e.ToTable("seo_url_research_competitor");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.Competitors).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchCompetitorHeading>(e =>
        {
            e.ToTable("seo_url_research_competitor_heading");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Competitor).WithMany(c => c.Headings).HasForeignKey(x => x.CompetitorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CompetitorId);
        });

        modelBuilder.Entity<SeoUrlResearchSourceHeading>(e =>
        {
            e.ToTable("seo_url_research_source_heading");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.SourceHeadings).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchTerm>(e =>
        {
            e.ToTable("seo_url_research_term");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.RecommendedTerms).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchClosingFaq>(e =>
        {
            e.ToTable("seo_url_research_closing_faq");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.ClosingFaqs).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoUrlResearchSectionHint>(e =>
        {
            e.ToTable("seo_url_research_section_hint");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.UrlResearch).WithMany(r => r.SectionHints).HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.SubtopicsFromSerp).HasColumnType("text[]");
            e.HasIndex(x => x.UrlResearchId);
        });

        modelBuilder.Entity<SeoSiteResearch>(e =>
        {
            e.ToTable("seo_site_research");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.DiscoveredUrlsJson).HasColumnType("jsonb");
            e.Property(x => x.InternalLinkMapJson).HasColumnType("jsonb");
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<SeoSiteResearchPage>(e =>
        {
            e.ToTable("seo_site_research_page");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteResearch).WithMany(r => r.Pages).HasForeignKey(x => x.SiteResearchId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.HeadingsJson).HasColumnType("jsonb");
            e.Property(x => x.JsonLdJson).HasColumnType("jsonb");
            e.HasIndex(x => x.SiteResearchId);
            e.HasIndex(x => new { x.SiteResearchId, x.Url }).IsUnique();
        });

        modelBuilder.Entity<SeoSiteAnalyzerStepRun>(e =>
        {
            e.ToTable("seo_site_analyzer_step_run");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteResearch).WithMany(r => r.StepRuns).HasForeignKey(x => x.SiteResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UrlResearch).WithMany().HasForeignKey(x => x.UrlResearchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SiteResearchId, x.StepNumber });
            e.HasIndex(x => new { x.UrlResearchId, x.StepNumber });
        });

        modelBuilder.Entity<SiteAnalysisCompetitor>(e =>
        {
            e.ToTable("site_analysis_competitors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.Competitors).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisEntity>(e =>
        {
            e.ToTable("site_analysis_entities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.AssociatedPillarIds).HasColumnType("uuid[]");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany(p => p.Entities).HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
        });

        modelBuilder.Entity<SiteAnalysisPillarPage>(e =>
        {
            e.ToTable("site_analysis_pillar_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.TopicsFound).HasColumnType("text[]");
            e.Property(x => x.GapsFound).HasColumnType("text[]");
            e.HasOne(x => x.Pillar).WithMany(p => p.ExistingPages).HasForeignKey(x => x.PillarId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.PillarId);
        });

        modelBuilder.Entity<SiteAnalysisProfileExtractedTool>(e =>
        {
            e.ToTable("extracted_tools");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.SiteAnalysisProfile).WithMany().HasForeignKey(x => x.SiteAnalysisProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.SiteAnalysisProfileId);
            e.HasIndex(x => x.SitePageId);
            e.HasIndex(x => new { x.SiteAnalysisProfileId, x.SitePageId, x.Name, x.Department, x.Body }).IsUnique();
        });
    }
}
