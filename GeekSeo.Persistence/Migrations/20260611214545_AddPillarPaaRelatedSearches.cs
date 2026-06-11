using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPillarPaaRelatedSearches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaaQuestionsJson",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelatedSearchesJson",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaaQuestionsJson",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.DropColumn(
                name: "RelatedSearchesJson",
                schema: "geek_seo",
                table: "niche_pillars");
        }
    }
}
