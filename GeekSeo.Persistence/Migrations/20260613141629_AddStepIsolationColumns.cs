using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStepIsolationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrawledUrlsJson",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepStatusesJson",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrawledUrlsJson",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "StepStatusesJson",
                schema: "geek_seo",
                table: "niche_profiles");
        }
    }
}
