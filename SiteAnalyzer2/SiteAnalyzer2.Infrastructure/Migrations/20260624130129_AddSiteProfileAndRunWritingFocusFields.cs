using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteProfileAndRunWritingFocusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorityPageUrls",
                schema: "sa2",
                table: "site_profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessSummary",
                schema: "sa2",
                table: "site_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetitorDomains",
                schema: "sa2",
                table: "site_profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoAnchorNodes",
                schema: "sa2",
                table: "site_profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NicheDescription",
                schema: "sa2",
                table: "site_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NicheTags",
                schema: "sa2",
                table: "site_profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryNiche",
                schema: "sa2",
                table: "site_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceAreaDescription",
                schema: "sa2",
                table: "site_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GapTopics",
                schema: "sa2",
                table: "analysis_runs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedPillarAngle",
                schema: "sa2",
                table: "analysis_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedPillarIntent",
                schema: "sa2",
                table: "analysis_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchedPillarTopic",
                schema: "sa2",
                table: "analysis_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WritingInstructions",
                schema: "sa2",
                table: "analysis_runs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorityPageUrls",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "BusinessSummary",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "CompetitorDomains",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "GeoAnchorNodes",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "NicheDescription",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "NicheTags",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "PrimaryNiche",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "ServiceAreaDescription",
                schema: "sa2",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "GapTopics",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "MatchedPillarAngle",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "MatchedPillarIntent",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "MatchedPillarTopic",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "WritingInstructions",
                schema: "sa2",
                table: "analysis_runs");
        }
    }
}
