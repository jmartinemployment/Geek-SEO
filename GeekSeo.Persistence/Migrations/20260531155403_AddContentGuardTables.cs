using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentGuardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_seo_competitor_pages_seo_serp_results_serp_result_id",
                schema: "geek_seo",
                table: "seo_competitor_pages");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_content_documents_seo_projects_project_id",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_content_performance_snapshots_seo_published_pages_publi",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_geo_mention_snapshots_seo_geo_tracking_queries_query_id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_organization_members_seo_organizations_org_id",
                schema: "geek_seo",
                table: "seo_organization_members");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_projects_seo_organizations_org_id",
                schema: "geek_seo",
                table: "seo_projects");

            migrationBuilder.DropForeignKey(
                name: "fk_seo_site_audit_pages_seo_site_audits_site_audit_id",
                schema: "geek_seo",
                table: "seo_site_audit_pages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_wordpress_connections",
                schema: "geek_seo",
                table: "seo_wordpress_connections");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_usage_counters",
                schema: "geek_seo",
                table: "seo_usage_counters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_topical_maps",
                schema: "geek_seo",
                table: "seo_topical_maps");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_subscriptions",
                schema: "geek_seo",
                table: "seo_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_site_page_inventory",
                schema: "geek_seo",
                table: "seo_site_page_inventory");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_site_audits",
                schema: "geek_seo",
                table: "seo_site_audits");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_site_audit_pages",
                schema: "geek_seo",
                table: "seo_site_audit_pages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_serp_results",
                schema: "geek_seo",
                table: "seo_serp_results");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_serp_deep_cache",
                schema: "geek_seo",
                table: "seo_serp_deep_cache");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_reports",
                schema: "geek_seo",
                table: "seo_reports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_rank_tracking",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_published_pages",
                schema: "geek_seo",
                table: "seo_published_pages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_projects",
                schema: "geek_seo",
                table: "seo_projects");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_plagiarism_checks",
                schema: "geek_seo",
                table: "seo_plagiarism_checks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_page_audits",
                schema: "geek_seo",
                table: "seo_page_audits");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_organizations",
                schema: "geek_seo",
                table: "seo_organizations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_organization_members",
                schema: "geek_seo",
                table: "seo_organization_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_keywords",
                schema: "geek_seo",
                table: "seo_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_keyword_clusters",
                schema: "geek_seo",
                table: "seo_keyword_clusters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_gsc_connections",
                schema: "geek_seo",
                table: "seo_gsc_connections");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_geo_tracking_queries",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_geo_mention_snapshots",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_ga4_connections",
                schema: "geek_seo",
                table: "seo_ga4_connections");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_content_performance_snapshots",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_content_documents",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_competitor_pages",
                schema: "geek_seo",
                table: "seo_competitor_pages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_cannibalization_issues",
                schema: "geek_seo",
                table: "seo_cannibalization_issues");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_bulk_jobs",
                schema: "geek_seo",
                table: "seo_bulk_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_brand_voices",
                schema: "geek_seo",
                table: "seo_brand_voices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_background_jobs",
                schema: "geek_seo",
                table: "seo_background_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_api_keys",
                schema: "geek_seo",
                table: "seo_api_keys");

            migrationBuilder.DropPrimaryKey(
                name: "pk_seo_alerts",
                schema: "geek_seo",
                table: "seo_alerts");

            migrationBuilder.RenameColumn(
                name: "username",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "site_url",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "SiteUrl");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "encryption_tag",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "EncryptionTag");

            migrationBuilder.RenameColumn(
                name: "encryption_iv",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "EncryptionIv");

            migrationBuilder.RenameColumn(
                name: "encrypted_app_password",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "EncryptedAppPassword");

            migrationBuilder.RenameColumn(
                name: "default_post_status",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "DefaultPostStatus");

            migrationBuilder.RenameColumn(
                name: "connected_at",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "ConnectedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_wordpress_connections_project_id",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "IX_seo_wordpress_connections_ProjectId");

            migrationBuilder.RenameColumn(
                name: "feature",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "Feature");

            migrationBuilder.RenameColumn(
                name: "count",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "period_start",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "PeriodStart");

            migrationBuilder.RenameIndex(
                name: "ix_seo_usage_counters_user_id_period_start_feature",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "IX_seo_usage_counters_UserId_PeriodStart_Feature");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "generated_at",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "GeneratedAt");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "content_gaps_json",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "ContentGapsJson");

            migrationBuilder.RenameColumn(
                name: "clusters_json",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "ClustersJson");

            migrationBuilder.RenameColumn(
                name: "tier",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "Tier");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "paypal_subscription_id",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "PaypalSubscriptionId");

            migrationBuilder.RenameColumn(
                name: "current_period_end",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "CurrentPeriodEnd");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_subscriptions_user_id",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "IX_seo_subscriptions_UserId");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "h1",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "H1");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "word_count",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "WordCount");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "crawled_at",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "CrawledAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_site_page_inventory_project_id_url",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "IX_seo_site_page_inventory_ProjectId_Url");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "started_at",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "pages_crawled",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "PagesCrawled");

            migrationBuilder.RenameColumn(
                name: "overall_score",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "OverallScore");

            migrationBuilder.RenameColumn(
                name: "error_message",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "score",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "site_audit_id",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "SiteAuditId");

            migrationBuilder.RenameColumn(
                name: "issues_json",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "IssuesJson");

            migrationBuilder.RenameColumn(
                name: "crawled_at",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "CrawledAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_site_audit_pages_site_audit_id",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "IX_seo_site_audit_pages_SiteAuditId");

            migrationBuilder.RenameColumn(
                name: "location",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "keyword",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "Keyword");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "serp_features_json",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "SerpFeaturesJson");

            migrationBuilder.RenameColumn(
                name: "results_json",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "ResultsJson");

            migrationBuilder.RenameColumn(
                name: "related_searches_json",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "RelatedSearchesJson");

            migrationBuilder.RenameColumn(
                name: "people_also_ask_json",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "PeopleAlsoAskJson");

            migrationBuilder.RenameColumn(
                name: "language_code",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "LanguageCode");

            migrationBuilder.RenameColumn(
                name: "fetched_at",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "FetchedAt");

            migrationBuilder.RenameColumn(
                name: "featured_snippet",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "FeaturedSnippet");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_serp_results_keyword_location_language_code",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "IX_seo_serp_results_Keyword_Location_LanguageCode");

            migrationBuilder.RenameColumn(
                name: "location",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "keyword",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "Keyword");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "term_matrix_json",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "TermMatrixJson");

            migrationBuilder.RenameColumn(
                name: "results_json",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "ResultsJson");

            migrationBuilder.RenameColumn(
                name: "result_count",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "ResultCount");

            migrationBuilder.RenameColumn(
                name: "fetched_at",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "FetchedAt");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_serp_deep_cache_keyword_location_result_count",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "IX_seo_serp_deep_cache_Keyword_Location_ResultCount");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "storage_path",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "StoragePath");

            migrationBuilder.RenameColumn(
                name: "report_type",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "ReportType");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "position",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Position");

            migrationBuilder.RenameColumn(
                name: "keyword",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Keyword");

            migrationBuilder.RenameColumn(
                name: "impressions",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Impressions");

            migrationBuilder.RenameColumn(
                name: "date",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "ctr",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Ctr");

            migrationBuilder.RenameColumn(
                name: "clicks",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Clicks");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "page_url",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "PageUrl");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "word_press_post_id",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "WordPressPostId");

            migrationBuilder.RenameColumn(
                name: "target_keyword",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "TargetKeyword");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "last_audit_at",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "LastAuditAt");

            migrationBuilder.RenameColumn(
                name: "document_id",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "DocumentId");

            migrationBuilder.RenameIndex(
                name: "ix_seo_published_pages_project_id_url",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "IX_seo_published_pages_ProjectId_Url");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "org_id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "OrgId");

            migrationBuilder.RenameColumn(
                name: "gsc_connected",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "GscConnected");

            migrationBuilder.RenameColumn(
                name: "default_location",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "DefaultLocation");

            migrationBuilder.RenameColumn(
                name: "default_language",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "DefaultLanguage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_projects_user_id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "IX_seo_projects_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_seo_projects_org_id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "IX_seo_projects_OrgId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "matches_json",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "MatchesJson");

            migrationBuilder.RenameColumn(
                name: "match_percent",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "MatchPercent");

            migrationBuilder.RenameColumn(
                name: "document_id",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "DocumentId");

            migrationBuilder.RenameColumn(
                name: "checked_at",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "CheckedAt");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "score",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "metadata_json",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "MetadataJson");

            migrationBuilder.RenameColumn(
                name: "issues_json",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "IssuesJson");

            migrationBuilder.RenameColumn(
                name: "audited_at",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "AuditedAt");

            migrationBuilder.RenameColumn(
                name: "slug",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_organizations_slug",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "IX_seo_organizations_Slug");

            migrationBuilder.RenameColumn(
                name: "role",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "joined_at",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "JoinedAt");

            migrationBuilder.RenameColumn(
                name: "invited_by",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "InvitedBy");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "org_id",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "OrgId");

            migrationBuilder.RenameColumn(
                name: "location",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "keyword",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "Keyword");

            migrationBuilder.RenameColumn(
                name: "intent",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "Intent");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "search_volume",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "SearchVolume");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "keyword_difficulty",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "KeywordDifficulty");

            migrationBuilder.RenameColumn(
                name: "cluster_id",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "ClusterId");

            migrationBuilder.RenameColumn(
                name: "cached_at",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "CachedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "pillar_keyword",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "PillarKeyword");

            migrationBuilder.RenameColumn(
                name: "keywords_json",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "KeywordsJson");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "average_volume",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "AverageVolume");

            migrationBuilder.RenameColumn(
                name: "average_difficulty",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "AverageDifficulty");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "site_url",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "SiteUrl");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "encryption_tag",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "EncryptionTag");

            migrationBuilder.RenameColumn(
                name: "encryption_iv",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "EncryptionIv");

            migrationBuilder.RenameColumn(
                name: "encrypted_refresh_token",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "EncryptedRefreshToken");

            migrationBuilder.RenameColumn(
                name: "connected_at",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "ConnectedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_gsc_connections_project_id",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "IX_seo_gsc_connections_ProjectId");

            migrationBuilder.RenameColumn(
                name: "enabled",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "Enabled");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "query_text",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "QueryText");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "platforms_json",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "PlatformsJson");

            migrationBuilder.RenameColumn(
                name: "snippet",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "Snippet");

            migrationBuilder.RenameColumn(
                name: "platform",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "Platform");

            migrationBuilder.RenameColumn(
                name: "mentioned",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "Mentioned");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "query_id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "QueryId");

            migrationBuilder.RenameColumn(
                name: "checked_at",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "CheckedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_geo_mention_snapshots_query_id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "IX_seo_geo_mention_snapshots_QueryId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "property_id",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "PropertyId");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "encryption_tag",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "EncryptionTag");

            migrationBuilder.RenameColumn(
                name: "encryption_iv",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "EncryptionIv");

            migrationBuilder.RenameColumn(
                name: "encrypted_refresh_token",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "EncryptedRefreshToken");

            migrationBuilder.RenameColumn(
                name: "connected_at",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "ConnectedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_ga4_connections_project_id",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "IX_seo_ga4_connections_ProjectId");

            migrationBuilder.RenameColumn(
                name: "position",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Position");

            migrationBuilder.RenameColumn(
                name: "impressions",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Impressions");

            migrationBuilder.RenameColumn(
                name: "date",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "ctr",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Ctr");

            migrationBuilder.RenameColumn(
                name: "clicks",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Clicks");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "published_page_id",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "PublishedPageId");

            migrationBuilder.RenameIndex(
                name: "ix_seo_content_performance_snapshots_published_page_id_date",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "IX_seo_content_performance_snapshots_PublishedPageId_Date");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "word_count",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "WordCount");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "target_location",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "TargetLocation");

            migrationBuilder.RenameColumn(
                name: "target_keyword",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "TargetKeyword");

            migrationBuilder.RenameColumn(
                name: "seo_score",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "SeoScore");

            migrationBuilder.RenameColumn(
                name: "score_components_json",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ScoreComponentsJson");

            migrationBuilder.RenameColumn(
                name: "published_word_count",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "PublishedWordCount");

            migrationBuilder.RenameColumn(
                name: "published_score",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "PublishedScore");

            migrationBuilder.RenameColumn(
                name: "published_at",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "last_scored_at",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "LastScoredAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "content_html",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ContentHtml");

            migrationBuilder.RenameColumn(
                name: "ai_detection_score",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "AiDetectionScore");

            migrationBuilder.RenameIndex(
                name: "ix_seo_content_documents_status",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "IX_seo_content_documents_Status");

            migrationBuilder.RenameIndex(
                name: "ix_seo_content_documents_user_id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "IX_seo_content_documents_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_seo_content_documents_project_id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "IX_seo_content_documents_ProjectId");

            migrationBuilder.RenameColumn(
                name: "url",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "domain",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "Domain");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "word_count",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "WordCount");

            migrationBuilder.RenameColumn(
                name: "terms_json",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "TermsJson");

            migrationBuilder.RenameColumn(
                name: "structured_data_types_json",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "StructuredDataTypesJson");

            migrationBuilder.RenameColumn(
                name: "serp_result_id",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "SerpResultId");

            migrationBuilder.RenameColumn(
                name: "meta_title",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "MetaTitle");

            migrationBuilder.RenameColumn(
                name: "meta_description",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "MetaDescription");

            migrationBuilder.RenameColumn(
                name: "internal_link_count",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "InternalLinkCount");

            migrationBuilder.RenameColumn(
                name: "images_missing_alt",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ImagesMissingAlt");

            migrationBuilder.RenameColumn(
                name: "image_count",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ImageCount");

            migrationBuilder.RenameColumn(
                name: "http_status",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "HttpStatus");

            migrationBuilder.RenameColumn(
                name: "headings_json",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "HeadingsJson");

            migrationBuilder.RenameColumn(
                name: "has_structured_data",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "HasStructuredData");

            migrationBuilder.RenameColumn(
                name: "external_link_count",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ExternalLinkCount");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "crawled_at",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "CrawledAt");

            migrationBuilder.RenameColumn(
                name: "content_text",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ContentText");

            migrationBuilder.RenameIndex(
                name: "ix_seo_competitor_pages_serp_result_id_url",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "IX_seo_competitor_pages_SerpResultId_Url");

            migrationBuilder.RenameColumn(
                name: "severity",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "Severity");

            migrationBuilder.RenameColumn(
                name: "keyword",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "Keyword");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "detected_at",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "DetectedAt");

            migrationBuilder.RenameColumn(
                name: "competing_urls_json",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "CompetingUrlsJson");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "total_count",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "TotalCount");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "keywords_json",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "KeywordsJson");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completed_count",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "CompletedCount");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "style_instructions",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "StyleInstructions");

            migrationBuilder.RenameColumn(
                name: "sample_text",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "SampleText");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "started_at",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "result_id",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "ResultId");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "progress_percent",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "ProgressPercent");

            migrationBuilder.RenameColumn(
                name: "payload_json",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "PayloadJson");

            migrationBuilder.RenameColumn(
                name: "job_type",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "JobType");

            migrationBuilder.RenameColumn(
                name: "error_message",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "CompletedAt");

            migrationBuilder.RenameIndex(
                name: "ix_seo_background_jobs_user_id_status",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "IX_seo_background_jobs_UserId_Status");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "key_prefix",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "KeyPrefix");

            migrationBuilder.RenameColumn(
                name: "key_hash",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "KeyHash");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "message",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "project_id",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "is_read",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "alert_type",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "AlertType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_wordpress_connections",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_usage_counters",
                schema: "geek_seo",
                table: "seo_usage_counters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_topical_maps",
                schema: "geek_seo",
                table: "seo_topical_maps",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_subscriptions",
                schema: "geek_seo",
                table: "seo_subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_site_page_inventory",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_site_audits",
                schema: "geek_seo",
                table: "seo_site_audits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_site_audit_pages",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_serp_results",
                schema: "geek_seo",
                table: "seo_serp_results",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_serp_deep_cache",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_reports",
                schema: "geek_seo",
                table: "seo_reports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_rank_tracking",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_published_pages",
                schema: "geek_seo",
                table: "seo_published_pages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_projects",
                schema: "geek_seo",
                table: "seo_projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_plagiarism_checks",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_page_audits",
                schema: "geek_seo",
                table: "seo_page_audits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_organizations",
                schema: "geek_seo",
                table: "seo_organizations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_organization_members",
                schema: "geek_seo",
                table: "seo_organization_members",
                columns: new[] { "OrgId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_keywords",
                schema: "geek_seo",
                table: "seo_keywords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_keyword_clusters",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_gsc_connections",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_geo_tracking_queries",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_geo_mention_snapshots",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_ga4_connections",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_content_performance_snapshots",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_content_documents",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_competitor_pages",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_cannibalization_issues",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_bulk_jobs",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_brand_voices",
                schema: "geek_seo",
                table: "seo_brand_voices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_background_jobs",
                schema: "geek_seo",
                table: "seo_background_jobs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_api_keys",
                schema: "geek_seo",
                table: "seo_api_keys",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_seo_alerts",
                schema: "geek_seo",
                table: "seo_alerts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "seo_content_guard_policies",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoPatch = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_content_guard_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seo_content_guard_runs",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PrePatchHtml = table.Column<string>(type: "text", nullable: true),
                    PatchedHtml = table.Column<string>(type: "text", nullable: true),
                    WordPressDraftPostId = table.Column<int>(type: "integer", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_content_guard_runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seo_topical_maps_ProjectId",
                schema: "geek_seo",
                table: "seo_topical_maps",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_guard_policies_ProjectId",
                schema: "geek_seo",
                table: "seo_content_guard_policies",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_guard_runs_ProjectId_Status",
                schema: "geek_seo",
                table: "seo_content_guard_runs",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_seo_competitor_pages_seo_serp_results_SerpResultId",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                column: "SerpResultId",
                principalSchema: "geek_seo",
                principalTable: "seo_serp_results",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_content_documents_seo_projects_ProjectId",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "ProjectId",
                principalSchema: "geek_seo",
                principalTable: "seo_projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_content_performance_snapshots_seo_published_pages_Publi~",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                column: "PublishedPageId",
                principalSchema: "geek_seo",
                principalTable: "seo_published_pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_geo_mention_snapshots_seo_geo_tracking_queries_QueryId",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                column: "QueryId",
                principalSchema: "geek_seo",
                principalTable: "seo_geo_tracking_queries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_organization_members_seo_organizations_OrgId",
                schema: "geek_seo",
                table: "seo_organization_members",
                column: "OrgId",
                principalSchema: "geek_seo",
                principalTable: "seo_organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_projects_seo_organizations_OrgId",
                schema: "geek_seo",
                table: "seo_projects",
                column: "OrgId",
                principalSchema: "geek_seo",
                principalTable: "seo_organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_seo_site_audit_pages_seo_site_audits_SiteAuditId",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                column: "SiteAuditId",
                principalSchema: "geek_seo",
                principalTable: "seo_site_audits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_seo_competitor_pages_seo_serp_results_SerpResultId",
                schema: "geek_seo",
                table: "seo_competitor_pages");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_content_documents_seo_projects_ProjectId",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_content_performance_snapshots_seo_published_pages_Publi~",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_geo_mention_snapshots_seo_geo_tracking_queries_QueryId",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_organization_members_seo_organizations_OrgId",
                schema: "geek_seo",
                table: "seo_organization_members");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_projects_seo_organizations_OrgId",
                schema: "geek_seo",
                table: "seo_projects");

            migrationBuilder.DropForeignKey(
                name: "FK_seo_site_audit_pages_seo_site_audits_SiteAuditId",
                schema: "geek_seo",
                table: "seo_site_audit_pages");

            migrationBuilder.DropTable(
                name: "seo_content_guard_policies",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_content_guard_runs",
                schema: "geek_seo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_wordpress_connections",
                schema: "geek_seo",
                table: "seo_wordpress_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_usage_counters",
                schema: "geek_seo",
                table: "seo_usage_counters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_topical_maps",
                schema: "geek_seo",
                table: "seo_topical_maps");

            migrationBuilder.DropIndex(
                name: "IX_seo_topical_maps_ProjectId",
                schema: "geek_seo",
                table: "seo_topical_maps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_subscriptions",
                schema: "geek_seo",
                table: "seo_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_site_page_inventory",
                schema: "geek_seo",
                table: "seo_site_page_inventory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_site_audits",
                schema: "geek_seo",
                table: "seo_site_audits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_site_audit_pages",
                schema: "geek_seo",
                table: "seo_site_audit_pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_serp_results",
                schema: "geek_seo",
                table: "seo_serp_results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_serp_deep_cache",
                schema: "geek_seo",
                table: "seo_serp_deep_cache");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_reports",
                schema: "geek_seo",
                table: "seo_reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_rank_tracking",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_published_pages",
                schema: "geek_seo",
                table: "seo_published_pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_projects",
                schema: "geek_seo",
                table: "seo_projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_plagiarism_checks",
                schema: "geek_seo",
                table: "seo_plagiarism_checks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_page_audits",
                schema: "geek_seo",
                table: "seo_page_audits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_organizations",
                schema: "geek_seo",
                table: "seo_organizations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_organization_members",
                schema: "geek_seo",
                table: "seo_organization_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_keywords",
                schema: "geek_seo",
                table: "seo_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_keyword_clusters",
                schema: "geek_seo",
                table: "seo_keyword_clusters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_gsc_connections",
                schema: "geek_seo",
                table: "seo_gsc_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_geo_tracking_queries",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_geo_mention_snapshots",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_ga4_connections",
                schema: "geek_seo",
                table: "seo_ga4_connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_content_performance_snapshots",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_content_documents",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_competitor_pages",
                schema: "geek_seo",
                table: "seo_competitor_pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_cannibalization_issues",
                schema: "geek_seo",
                table: "seo_cannibalization_issues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_bulk_jobs",
                schema: "geek_seo",
                table: "seo_bulk_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_brand_voices",
                schema: "geek_seo",
                table: "seo_brand_voices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_background_jobs",
                schema: "geek_seo",
                table: "seo_background_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_api_keys",
                schema: "geek_seo",
                table: "seo_api_keys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_seo_alerts",
                schema: "geek_seo",
                table: "seo_alerts");

            migrationBuilder.RenameColumn(
                name: "Username",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SiteUrl",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "site_url");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "EncryptionTag",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "encryption_tag");

            migrationBuilder.RenameColumn(
                name: "EncryptionIv",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "encryption_iv");

            migrationBuilder.RenameColumn(
                name: "EncryptedAppPassword",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "encrypted_app_password");

            migrationBuilder.RenameColumn(
                name: "DefaultPostStatus",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "default_post_status");

            migrationBuilder.RenameColumn(
                name: "ConnectedAt",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "connected_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_wordpress_connections_ProjectId",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                newName: "ix_seo_wordpress_connections_project_id");

            migrationBuilder.RenameColumn(
                name: "Feature",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "feature");

            migrationBuilder.RenameColumn(
                name: "Count",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "count");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "PeriodStart",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "period_start");

            migrationBuilder.RenameIndex(
                name: "IX_seo_usage_counters_UserId_PeriodStart_Feature",
                schema: "geek_seo",
                table: "seo_usage_counters",
                newName: "ix_seo_usage_counters_user_id_period_start_feature");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "GeneratedAt",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "generated_at");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "ContentGapsJson",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "content_gaps_json");

            migrationBuilder.RenameColumn(
                name: "ClustersJson",
                schema: "geek_seo",
                table: "seo_topical_maps",
                newName: "clusters_json");

            migrationBuilder.RenameColumn(
                name: "Tier",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "tier");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PaypalSubscriptionId",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "paypal_subscription_id");

            migrationBuilder.RenameColumn(
                name: "CurrentPeriodEnd",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "current_period_end");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_subscriptions_UserId",
                schema: "geek_seo",
                table: "seo_subscriptions",
                newName: "ix_seo_subscriptions_user_id");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "H1",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "h1");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WordCount",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "word_count");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "CrawledAt",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "crawled_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_site_page_inventory_ProjectId_Url",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                newName: "ix_seo_site_page_inventory_project_id_url");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "PagesCrawled",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "pages_crawled");

            migrationBuilder.RenameColumn(
                name: "OverallScore",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "overall_score");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "geek_seo",
                table: "seo_site_audits",
                newName: "completed_at");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Score",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "score");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SiteAuditId",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "site_audit_id");

            migrationBuilder.RenameColumn(
                name: "IssuesJson",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "issues_json");

            migrationBuilder.RenameColumn(
                name: "CrawledAt",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "crawled_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_site_audit_pages_SiteAuditId",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                newName: "ix_seo_site_audit_pages_site_audit_id");

            migrationBuilder.RenameColumn(
                name: "Location",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "location");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "keyword");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SerpFeaturesJson",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "serp_features_json");

            migrationBuilder.RenameColumn(
                name: "ResultsJson",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "results_json");

            migrationBuilder.RenameColumn(
                name: "RelatedSearchesJson",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "related_searches_json");

            migrationBuilder.RenameColumn(
                name: "PeopleAlsoAskJson",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "people_also_ask_json");

            migrationBuilder.RenameColumn(
                name: "LanguageCode",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "language_code");

            migrationBuilder.RenameColumn(
                name: "FetchedAt",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "fetched_at");

            migrationBuilder.RenameColumn(
                name: "FeaturedSnippet",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "featured_snippet");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "expires_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_serp_results_Keyword_Location_LanguageCode",
                schema: "geek_seo",
                table: "seo_serp_results",
                newName: "ix_seo_serp_results_keyword_location_language_code");

            migrationBuilder.RenameColumn(
                name: "Location",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "location");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "keyword");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TermMatrixJson",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "term_matrix_json");

            migrationBuilder.RenameColumn(
                name: "ResultsJson",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "results_json");

            migrationBuilder.RenameColumn(
                name: "ResultCount",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "result_count");

            migrationBuilder.RenameColumn(
                name: "FetchedAt",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "fetched_at");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "expires_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_serp_deep_cache_Keyword_Location_ResultCount",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                newName: "ix_seo_serp_deep_cache_keyword_location_result_count");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "StoragePath",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "storage_path");

            migrationBuilder.RenameColumn(
                name: "ReportType",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "report_type");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_reports",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Position",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "position");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "keyword");

            migrationBuilder.RenameColumn(
                name: "Impressions",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "impressions");

            migrationBuilder.RenameColumn(
                name: "Date",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Ctr",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "ctr");

            migrationBuilder.RenameColumn(
                name: "Clicks",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "clicks");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "PageUrl",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                newName: "page_url");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WordPressPostId",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "word_press_post_id");

            migrationBuilder.RenameColumn(
                name: "TargetKeyword",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "target_keyword");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "LastAuditAt",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "last_audit_at");

            migrationBuilder.RenameColumn(
                name: "DocumentId",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "document_id");

            migrationBuilder.RenameIndex(
                name: "IX_seo_published_pages_ProjectId_Url",
                schema: "geek_seo",
                table: "seo_published_pages",
                newName: "ix_seo_published_pages_project_id_url");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "OrgId",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "org_id");

            migrationBuilder.RenameColumn(
                name: "GscConnected",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "gsc_connected");

            migrationBuilder.RenameColumn(
                name: "DefaultLocation",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "default_location");

            migrationBuilder.RenameColumn(
                name: "DefaultLanguage",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "default_language");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_projects_UserId",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "ix_seo_projects_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_seo_projects_OrgId",
                schema: "geek_seo",
                table: "seo_projects",
                newName: "ix_seo_projects_org_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "MatchesJson",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "matches_json");

            migrationBuilder.RenameColumn(
                name: "MatchPercent",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "match_percent");

            migrationBuilder.RenameColumn(
                name: "DocumentId",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "document_id");

            migrationBuilder.RenameColumn(
                name: "CheckedAt",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                newName: "checked_at");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Score",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "score");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "MetadataJson",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "metadata_json");

            migrationBuilder.RenameColumn(
                name: "IssuesJson",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "issues_json");

            migrationBuilder.RenameColumn(
                name: "AuditedAt",
                schema: "geek_seo",
                table: "seo_page_audits",
                newName: "audited_at");

            migrationBuilder.RenameColumn(
                name: "Slug",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "owner_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_organizations_Slug",
                schema: "geek_seo",
                table: "seo_organizations",
                newName: "ix_seo_organizations_slug");

            migrationBuilder.RenameColumn(
                name: "Role",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "joined_at");

            migrationBuilder.RenameColumn(
                name: "InvitedBy",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "invited_by");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "OrgId",
                schema: "geek_seo",
                table: "seo_organization_members",
                newName: "org_id");

            migrationBuilder.RenameColumn(
                name: "Location",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "location");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "keyword");

            migrationBuilder.RenameColumn(
                name: "Intent",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "intent");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SearchVolume",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "search_volume");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "KeywordDifficulty",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "keyword_difficulty");

            migrationBuilder.RenameColumn(
                name: "ClusterId",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "cluster_id");

            migrationBuilder.RenameColumn(
                name: "CachedAt",
                schema: "geek_seo",
                table: "seo_keywords",
                newName: "cached_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "PillarKeyword",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "pillar_keyword");

            migrationBuilder.RenameColumn(
                name: "KeywordsJson",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "keywords_json");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AverageVolume",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "average_volume");

            migrationBuilder.RenameColumn(
                name: "AverageDifficulty",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                newName: "average_difficulty");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SiteUrl",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "site_url");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "EncryptionTag",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "encryption_tag");

            migrationBuilder.RenameColumn(
                name: "EncryptionIv",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "encryption_iv");

            migrationBuilder.RenameColumn(
                name: "EncryptedRefreshToken",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "encrypted_refresh_token");

            migrationBuilder.RenameColumn(
                name: "ConnectedAt",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "connected_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_gsc_connections_ProjectId",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                newName: "ix_seo_gsc_connections_project_id");

            migrationBuilder.RenameColumn(
                name: "Enabled",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "enabled");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QueryText",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "query_text");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "PlatformsJson",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                newName: "platforms_json");

            migrationBuilder.RenameColumn(
                name: "Snippet",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "snippet");

            migrationBuilder.RenameColumn(
                name: "Platform",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "platform");

            migrationBuilder.RenameColumn(
                name: "Mentioned",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "mentioned");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "QueryId",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "query_id");

            migrationBuilder.RenameColumn(
                name: "CheckedAt",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "checked_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_geo_mention_snapshots_QueryId",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                newName: "ix_seo_geo_mention_snapshots_query_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "property_id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "EncryptionTag",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "encryption_tag");

            migrationBuilder.RenameColumn(
                name: "EncryptionIv",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "encryption_iv");

            migrationBuilder.RenameColumn(
                name: "EncryptedRefreshToken",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "encrypted_refresh_token");

            migrationBuilder.RenameColumn(
                name: "ConnectedAt",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "connected_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_ga4_connections_ProjectId",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                newName: "ix_seo_ga4_connections_project_id");

            migrationBuilder.RenameColumn(
                name: "Position",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "position");

            migrationBuilder.RenameColumn(
                name: "Impressions",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "impressions");

            migrationBuilder.RenameColumn(
                name: "Date",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Ctr",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "ctr");

            migrationBuilder.RenameColumn(
                name: "Clicks",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "clicks");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PublishedPageId",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "published_page_id");

            migrationBuilder.RenameIndex(
                name: "IX_seo_content_performance_snapshots_PublishedPageId_Date",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                newName: "ix_seo_content_performance_snapshots_published_page_id_date");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WordCount",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "word_count");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TargetLocation",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "target_location");

            migrationBuilder.RenameColumn(
                name: "TargetKeyword",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "target_keyword");

            migrationBuilder.RenameColumn(
                name: "SeoScore",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "seo_score");

            migrationBuilder.RenameColumn(
                name: "ScoreComponentsJson",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "score_components_json");

            migrationBuilder.RenameColumn(
                name: "PublishedWordCount",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "published_word_count");

            migrationBuilder.RenameColumn(
                name: "PublishedScore",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "published_score");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "published_at");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "LastScoredAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "last_scored_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContentHtml",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "content_html");

            migrationBuilder.RenameColumn(
                name: "AiDetectionScore",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ai_detection_score");

            migrationBuilder.RenameIndex(
                name: "IX_seo_content_documents_Status",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ix_seo_content_documents_status");

            migrationBuilder.RenameIndex(
                name: "IX_seo_content_documents_UserId",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ix_seo_content_documents_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_seo_content_documents_ProjectId",
                schema: "geek_seo",
                table: "seo_content_documents",
                newName: "ix_seo_content_documents_project_id");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Domain",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "domain");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WordCount",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "word_count");

            migrationBuilder.RenameColumn(
                name: "TermsJson",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "terms_json");

            migrationBuilder.RenameColumn(
                name: "StructuredDataTypesJson",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "structured_data_types_json");

            migrationBuilder.RenameColumn(
                name: "SerpResultId",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "serp_result_id");

            migrationBuilder.RenameColumn(
                name: "MetaTitle",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "meta_title");

            migrationBuilder.RenameColumn(
                name: "MetaDescription",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "meta_description");

            migrationBuilder.RenameColumn(
                name: "InternalLinkCount",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "internal_link_count");

            migrationBuilder.RenameColumn(
                name: "ImagesMissingAlt",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "images_missing_alt");

            migrationBuilder.RenameColumn(
                name: "ImageCount",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "image_count");

            migrationBuilder.RenameColumn(
                name: "HttpStatus",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "http_status");

            migrationBuilder.RenameColumn(
                name: "HeadingsJson",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "headings_json");

            migrationBuilder.RenameColumn(
                name: "HasStructuredData",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "has_structured_data");

            migrationBuilder.RenameColumn(
                name: "ExternalLinkCount",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "external_link_count");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CrawledAt",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "crawled_at");

            migrationBuilder.RenameColumn(
                name: "ContentText",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "content_text");

            migrationBuilder.RenameIndex(
                name: "IX_seo_competitor_pages_SerpResultId_Url",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                newName: "ix_seo_competitor_pages_serp_result_id_url");

            migrationBuilder.RenameColumn(
                name: "Severity",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "severity");

            migrationBuilder.RenameColumn(
                name: "Keyword",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "keyword");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "DetectedAt",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "detected_at");

            migrationBuilder.RenameColumn(
                name: "CompetingUrlsJson",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                newName: "competing_urls_json");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "TotalCount",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "total_count");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "KeywordsJson",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "keywords_json");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletedCount",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "completed_count");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                newName: "completed_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "StyleInstructions",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "style_instructions");

            migrationBuilder.RenameColumn(
                name: "SampleText",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "sample_text");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_brand_voices",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "started_at");

            migrationBuilder.RenameColumn(
                name: "ResultId",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "result_id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "ProgressPercent",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "progress_percent");

            migrationBuilder.RenameColumn(
                name: "PayloadJson",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "payload_json");

            migrationBuilder.RenameColumn(
                name: "JobType",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "job_type");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "completed_at");

            migrationBuilder.RenameIndex(
                name: "IX_seo_background_jobs_UserId_Status",
                schema: "geek_seo",
                table: "seo_background_jobs",
                newName: "ix_seo_background_jobs_user_id_status");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "KeyPrefix",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "key_prefix");

            migrationBuilder.RenameColumn(
                name: "KeyHash",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "key_hash");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_api_keys",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Message",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "project_id");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "is_read");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AlertType",
                schema: "geek_seo",
                table: "seo_alerts",
                newName: "alert_type");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_wordpress_connections",
                schema: "geek_seo",
                table: "seo_wordpress_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_usage_counters",
                schema: "geek_seo",
                table: "seo_usage_counters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_topical_maps",
                schema: "geek_seo",
                table: "seo_topical_maps",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_subscriptions",
                schema: "geek_seo",
                table: "seo_subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_site_page_inventory",
                schema: "geek_seo",
                table: "seo_site_page_inventory",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_site_audits",
                schema: "geek_seo",
                table: "seo_site_audits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_site_audit_pages",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_serp_results",
                schema: "geek_seo",
                table: "seo_serp_results",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_serp_deep_cache",
                schema: "geek_seo",
                table: "seo_serp_deep_cache",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_reports",
                schema: "geek_seo",
                table: "seo_reports",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_rank_tracking",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_published_pages",
                schema: "geek_seo",
                table: "seo_published_pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_projects",
                schema: "geek_seo",
                table: "seo_projects",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_plagiarism_checks",
                schema: "geek_seo",
                table: "seo_plagiarism_checks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_page_audits",
                schema: "geek_seo",
                table: "seo_page_audits",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_organizations",
                schema: "geek_seo",
                table: "seo_organizations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_organization_members",
                schema: "geek_seo",
                table: "seo_organization_members",
                columns: new[] { "org_id", "user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_keywords",
                schema: "geek_seo",
                table: "seo_keywords",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_keyword_clusters",
                schema: "geek_seo",
                table: "seo_keyword_clusters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_gsc_connections",
                schema: "geek_seo",
                table: "seo_gsc_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_geo_tracking_queries",
                schema: "geek_seo",
                table: "seo_geo_tracking_queries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_geo_mention_snapshots",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_ga4_connections",
                schema: "geek_seo",
                table: "seo_ga4_connections",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_content_performance_snapshots",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_content_documents",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_competitor_pages",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_cannibalization_issues",
                schema: "geek_seo",
                table: "seo_cannibalization_issues",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_bulk_jobs",
                schema: "geek_seo",
                table: "seo_bulk_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_brand_voices",
                schema: "geek_seo",
                table: "seo_brand_voices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_background_jobs",
                schema: "geek_seo",
                table: "seo_background_jobs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_api_keys",
                schema: "geek_seo",
                table: "seo_api_keys",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_seo_alerts",
                schema: "geek_seo",
                table: "seo_alerts",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_seo_competitor_pages_seo_serp_results_serp_result_id",
                schema: "geek_seo",
                table: "seo_competitor_pages",
                column: "serp_result_id",
                principalSchema: "geek_seo",
                principalTable: "seo_serp_results",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_content_documents_seo_projects_project_id",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "project_id",
                principalSchema: "geek_seo",
                principalTable: "seo_projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_content_performance_snapshots_seo_published_pages_publi",
                schema: "geek_seo",
                table: "seo_content_performance_snapshots",
                column: "published_page_id",
                principalSchema: "geek_seo",
                principalTable: "seo_published_pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_geo_mention_snapshots_seo_geo_tracking_queries_query_id",
                schema: "geek_seo",
                table: "seo_geo_mention_snapshots",
                column: "query_id",
                principalSchema: "geek_seo",
                principalTable: "seo_geo_tracking_queries",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_organization_members_seo_organizations_org_id",
                schema: "geek_seo",
                table: "seo_organization_members",
                column: "org_id",
                principalSchema: "geek_seo",
                principalTable: "seo_organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_projects_seo_organizations_org_id",
                schema: "geek_seo",
                table: "seo_projects",
                column: "org_id",
                principalSchema: "geek_seo",
                principalTable: "seo_organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_seo_site_audit_pages_seo_site_audits_site_audit_id",
                schema: "geek_seo",
                table: "seo_site_audit_pages",
                column: "site_audit_id",
                principalSchema: "geek_seo",
                principalTable: "seo_site_audits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
