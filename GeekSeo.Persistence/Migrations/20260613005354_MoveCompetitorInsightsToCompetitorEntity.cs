using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCompetitorInsightsToCompetitorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitorInsightsJson",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.AddColumn<string>(
                name: "AreaServedJson",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvgWordCount",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BrandName",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasFaqSchema",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KnowsAboutJson",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PagesCrawled",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SameAsJson",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServicesJson",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaServedJson",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "AvgWordCount",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "BrandName",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "HasFaqSchema",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "KnowsAboutJson",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "PagesCrawled",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "SameAsJson",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "ServicesJson",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.AddColumn<string>(
                name: "CompetitorInsightsJson",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
