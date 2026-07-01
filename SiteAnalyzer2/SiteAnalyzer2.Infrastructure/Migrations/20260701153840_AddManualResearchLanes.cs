using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualResearchLanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResearchLane",
                schema: "sa2",
                table: "serp_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchMode",
                schema: "sa2",
                table: "analysis_runs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "sa2");

            migrationBuilder.AddColumn<string>(
                name: "TopicSlug",
                schema: "sa2",
                table: "analysis_runs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_serp_items_RunId_ResearchLane",
                schema: "sa2",
                table: "serp_items",
                columns: new[] { "RunId", "ResearchLane" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_serp_items_RunId_ResearchLane",
                schema: "sa2",
                table: "serp_items");

            migrationBuilder.DropColumn(
                name: "ResearchLane",
                schema: "sa2",
                table: "serp_items");

            migrationBuilder.DropColumn(
                name: "ResearchMode",
                schema: "sa2",
                table: "analysis_runs");

            migrationBuilder.DropColumn(
                name: "TopicSlug",
                schema: "sa2",
                table: "analysis_runs");
        }
    }
}
