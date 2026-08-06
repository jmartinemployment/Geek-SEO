using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteAnalysisPageSectionTrees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_analysis_page_section_trees",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    TreeJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_page_section_trees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_page_section_trees_site_analysis_profiles_Sit~",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_url_pattern_topics_SiteAnalysisProfi~1",
                schema: "geek_seo",
                table: "site_analysis_profile_url_pattern_topics",
                columns: new[] { "SiteAnalysisProfileId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_page_section_trees_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_page_section_trees",
                column: "SiteAnalysisProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_analysis_page_section_trees",
                schema: "geek_seo");

            migrationBuilder.DropIndex(
                name: "IX_site_analysis_profile_url_pattern_topics_SiteAnalysisProfi~1",
                schema: "geek_seo",
                table: "site_analysis_profile_url_pattern_topics");
        }
    }
}
