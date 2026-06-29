using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisRunCompetitorCrawlStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompetitorCrawlFinishedAt",
                schema: "sa2",
                table: "analysis_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetitorCrawlMessage",
                schema: "sa2",
                table: "analysis_runs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompetitorCrawlStartedAt",
                schema: "sa2",
                table: "analysis_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetitorCrawlStatus",
                schema: "sa2",
                table: "analysis_runs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitorCrawlFinishedAt",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "CompetitorCrawlMessage",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "CompetitorCrawlStartedAt",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "CompetitorCrawlStatus",
                schema: "sa2",
                table: "analysis_runs");
        }
    }
}
