using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModernizeAnalysisRunStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE sa2.analysis_runs
                SET "Status" = 'SerpReady'
                WHERE "Status" = 'AwaitingConfirmation';

                UPDATE sa2.analysis_runs ar
                SET "Status" = 'ResearchReady'
                WHERE "GapTopics" IS NOT NULL
                  AND "GapTopics" <> '[]'
                  AND "Status" IN ('SerpReady', 'AwaitingConfirmation', 'Completed');

                UPDATE sa2.analysis_runs ar
                SET "Status" = 'ResearchFailed'
                WHERE "Status" IN ('SerpReady', 'AwaitingConfirmation')
                  AND EXISTS (
                      SELECT 1 FROM sa2.competitor_pages cp WHERE cp."RunId" = ar."Id")
                  AND ("GapTopics" IS NULL OR "GapTopics" = '[]');

                UPDATE sa2.analysis_runs ar
                SET "CompetitorCrawlStatus" = 'pages_saved'
                WHERE "CompetitorCrawlStatus" = 'failed'
                  AND EXISTS (
                      SELECT 1 FROM sa2.competitor_pages cp WHERE cp."RunId" = ar."Id");

                UPDATE sa2.analysis_runs ar
                SET "CompetitorCrawlStatus" = 'complete'
                WHERE "GapTopics" IS NOT NULL
                  AND "GapTopics" <> '[]'
                  AND EXISTS (
                      SELECT 1 FROM sa2.competitor_pages cp WHERE cp."RunId" = ar."Id");

                UPDATE sa2.analysis_runs
                SET "CurrentStage" = NULL
                WHERE "Status" IN ('SerpReady', 'ResearchReady', 'ResearchFailed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE sa2.analysis_runs
                SET "Status" = 'AwaitingConfirmation'
                WHERE "Status" = 'SerpReady';

                UPDATE sa2.analysis_runs
                SET "CompetitorCrawlStatus" = 'failed'
                WHERE "CompetitorCrawlStatus" = 'pages_saved';
                """);
        }
    }
}
