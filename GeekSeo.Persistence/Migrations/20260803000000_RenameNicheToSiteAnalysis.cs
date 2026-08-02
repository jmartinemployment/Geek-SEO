using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations;

/// <summary>
/// Renames niche_* tables/columns to site_analysis_* / focus naming.
/// Idempotent via IF EXISTS / to_regclass guards.
/// </summary>
public partial class RenameNicheToSiteAnalysis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION pg_temp.rename_niche_child(old_name text, new_name text)
            RETURNS void
            LANGUAGE plpgsql
            AS $fn$
            BEGIN
              IF to_regclass(format('geek_seo.%I', old_name)) IS NOT NULL THEN
                EXECUTE format('ALTER TABLE geek_seo.%I RENAME TO %I', old_name, new_name);
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo'
                  AND table_name = new_name
                  AND column_name = 'NicheProfileId'
              ) THEN
                EXECUTE format(
                  'ALTER TABLE geek_seo.%I RENAME COLUMN %I TO %I',
                  new_name, 'NicheProfileId', 'SiteAnalysisProfileId');
              END IF;
            END
            $fn$;

            DO $$
            BEGIN
              IF to_regclass('geek_seo.niche_profiles') IS NOT NULL THEN
                ALTER TABLE geek_seo.niche_profiles RENAME TO site_analysis_profiles;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'site_analysis_profiles' AND column_name = 'PrimaryNiche'
              ) THEN
                ALTER TABLE geek_seo.site_analysis_profiles RENAME COLUMN "PrimaryNiche" TO "PrimaryFocus";
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'site_analysis_profiles' AND column_name = 'NicheDescription'
              ) THEN
                ALTER TABLE geek_seo.site_analysis_profiles RENAME COLUMN "NicheDescription" TO "FocusDescription";
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'site_analysis_profiles' AND column_name = 'NicheTags'
              ) THEN
                ALTER TABLE geek_seo.site_analysis_profiles RENAME COLUMN "NicheTags" TO "FocusTags";
              END IF;

              PERFORM pg_temp.rename_niche_child('niche_competitors', 'site_analysis_competitors');
              PERFORM pg_temp.rename_niche_child('niche_entities', 'site_analysis_entities');
              PERFORM pg_temp.rename_niche_child('niche_pillars', 'site_analysis_pillars');
              PERFORM pg_temp.rename_niche_child('niche_subtopics', 'site_analysis_subtopics');
              PERFORM pg_temp.rename_niche_child('niche_pillar_pages', 'site_analysis_pillar_pages');
              PERFORM pg_temp.rename_niche_child('niche_topic_candidates', 'site_analysis_topic_candidates');
              PERFORM pg_temp.rename_niche_child('niche_topic_candidate_evidence', 'site_analysis_topic_candidate_evidence');
              PERFORM pg_temp.rename_niche_child('niche_profile_step_runs', 'site_analysis_profile_step_runs');
              PERFORM pg_temp.rename_niche_child('niche_profile_schema_signals', 'site_analysis_profile_schema_signals');
              PERFORM pg_temp.rename_niche_child('niche_profile_discovered_urls', 'site_analysis_profile_discovered_urls');
              PERFORM pg_temp.rename_niche_child('niche_profile_navigation_links', 'site_analysis_profile_navigation_links');
              PERFORM pg_temp.rename_niche_child('niche_profile_headings', 'site_analysis_profile_headings');
              PERFORM pg_temp.rename_niche_child('niche_profile_page_content_items', 'site_analysis_profile_page_content_items');
              PERFORM pg_temp.rename_niche_child('niche_profile_page_content_meta', 'site_analysis_profile_page_content_meta');
              PERFORM pg_temp.rename_niche_child('niche_profile_site_pages', 'site_analysis_profile_site_pages');
              PERFORM pg_temp.rename_niche_child('niche_profile_site_page_links', 'site_analysis_profile_site_page_links');
              PERFORM pg_temp.rename_niche_child('niche_profile_url_pattern_topics', 'site_analysis_profile_url_pattern_topics');
              PERFORM pg_temp.rename_niche_child('niche_profile_site_crawl_meta', 'site_analysis_profile_site_crawl_meta');

              IF to_regclass('geek_seo.seo_content_documents') IS NOT NULL THEN
                UPDATE geek_seo.seo_content_documents
                SET "SiteFocusJson" = replace(
                      replace(
                        replace(
                          replace("SiteFocusJson", '"primaryNiche"', '"primaryFocus"'),
                          '"nicheDescription"', '"focusDescription"'),
                        '"nicheTags"', '"focusTags"'),
                      '"nicheProfileId"', '"siteAnalysisProfileId"')
                WHERE "SiteFocusJson" IS NOT NULL
                  AND (
                    "SiteFocusJson" LIKE '%primaryNiche%'
                    OR "SiteFocusJson" LIKE '%nicheDescription%'
                    OR "SiteFocusJson" LIKE '%nicheTags%'
                    OR "SiteFocusJson" LIKE '%nicheProfileId%'
                  );
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION pg_temp.rename_site_child(old_name text, new_name text)
            RETURNS void
            LANGUAGE plpgsql
            AS $fn$
            BEGIN
              IF to_regclass(format('geek_seo.%I', old_name)) IS NOT NULL THEN
                EXECUTE format('ALTER TABLE geek_seo.%I RENAME TO %I', old_name, new_name);
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo'
                  AND table_name = new_name
                  AND column_name = 'SiteAnalysisProfileId'
              ) THEN
                EXECUTE format(
                  'ALTER TABLE geek_seo.%I RENAME COLUMN %I TO %I',
                  new_name, 'SiteAnalysisProfileId', 'NicheProfileId');
              END IF;
            END
            $fn$;

            DO $$
            BEGIN
              PERFORM pg_temp.rename_site_child('site_analysis_competitors', 'niche_competitors');
              PERFORM pg_temp.rename_site_child('site_analysis_entities', 'niche_entities');
              PERFORM pg_temp.rename_site_child('site_analysis_pillars', 'niche_pillars');
              PERFORM pg_temp.rename_site_child('site_analysis_subtopics', 'niche_subtopics');
              PERFORM pg_temp.rename_site_child('site_analysis_pillar_pages', 'niche_pillar_pages');
              PERFORM pg_temp.rename_site_child('site_analysis_topic_candidates', 'niche_topic_candidates');
              PERFORM pg_temp.rename_site_child('site_analysis_topic_candidate_evidence', 'niche_topic_candidate_evidence');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_step_runs', 'niche_profile_step_runs');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_schema_signals', 'niche_profile_schema_signals');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_discovered_urls', 'niche_profile_discovered_urls');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_navigation_links', 'niche_profile_navigation_links');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_headings', 'niche_profile_headings');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_page_content_items', 'niche_profile_page_content_items');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_page_content_meta', 'niche_profile_page_content_meta');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_site_pages', 'niche_profile_site_pages');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_site_page_links', 'niche_profile_site_page_links');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_url_pattern_topics', 'niche_profile_url_pattern_topics');
              PERFORM pg_temp.rename_site_child('site_analysis_profile_site_crawl_meta', 'niche_profile_site_crawl_meta');

              IF to_regclass('geek_seo.site_analysis_profiles') IS NOT NULL THEN
                ALTER TABLE geek_seo.site_analysis_profiles RENAME TO niche_profiles;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'niche_profiles' AND column_name = 'PrimaryFocus'
              ) THEN
                ALTER TABLE geek_seo.niche_profiles RENAME COLUMN "PrimaryFocus" TO "PrimaryNiche";
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'niche_profiles' AND column_name = 'FocusDescription'
              ) THEN
                ALTER TABLE geek_seo.niche_profiles RENAME COLUMN "FocusDescription" TO "NicheDescription";
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'geek_seo' AND table_name = 'niche_profiles' AND column_name = 'FocusTags'
              ) THEN
                ALTER TABLE geek_seo.niche_profiles RENAME COLUMN "FocusTags" TO "NicheTags";
              END IF;
            END $$;
            """);
    }
}
