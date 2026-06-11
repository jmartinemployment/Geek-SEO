using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalSerpColumnsAndCompetitorScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalPaaQuestionsJson",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocalRelatedSearchesJson",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalPaaQuestionsJson",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.DropColumn(
                name: "LocalRelatedSearchesJson",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "geek_seo",
                table: "niche_competitors");
        }
    }
}
