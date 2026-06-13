using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorPillarsAndAnalyzedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompetitorAnalyzedAt",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PillarsJson",
                schema: "geek_seo",
                table: "niche_competitors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitorAnalyzedAt",
                schema: "geek_seo",
                table: "niche_competitors");

            migrationBuilder.DropColumn(
                name: "PillarsJson",
                schema: "geek_seo",
                table: "niche_competitors");
        }
    }
}
