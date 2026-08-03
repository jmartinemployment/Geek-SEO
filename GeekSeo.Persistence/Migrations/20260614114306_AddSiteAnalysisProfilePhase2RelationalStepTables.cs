using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteAnalysisProfilePhase2RelationalStepTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_analysis_profile_page_content_items",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    ItemKind = table.Column<string>(type: "text", nullable: false),
                    ItemText = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_page_content_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_page_content_items_site_analysis_profiles_SiteAnalysisProfileId~",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_profile_page_content_meta",
                schema: "geek_seo",
                columns: table => new
                {
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    ListItemsScanned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_page_content_meta", x => x.SiteAnalysisProfileId);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_page_content_meta_site_analysis_profiles_SiteAnalysisProfile~",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_profile_site_crawl_meta",
                schema: "geek_seo",
                columns: table => new
                {
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PagesAttempted = table.Column<int>(type: "integer", nullable: false),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_site_crawl_meta", x => x.SiteAnalysisProfileId);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_site_crawl_meta_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_profile_site_page_links",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    TargetUrl = table.Column<string>(type: "text", nullable: false),
                    AnchorText = table.Column<string>(type: "text", nullable: false),
                    InferredFromUrlSlug = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_site_page_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_site_page_links_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_profile_site_pages",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FetchMethod = table.Column<string>(type: "text", nullable: false),
                    VisibleText = table.Column<string>(type: "text", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_site_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_site_pages_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_profile_url_pattern_topics",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    PathSegment = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profile_url_pattern_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_profile_url_pattern_topics_site_analysis_profiles_SiteAnalysisProfileId~",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_page_content_items_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_profile_page_content_items",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_site_page_links_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_profile_site_page_links",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_site_pages_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_site_pages_SiteAnalysisProfileId_Url",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                columns: new[] { "SiteAnalysisProfileId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_url_pattern_topics_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_profile_url_pattern_topics",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profile_url_pattern_topics_SiteAnalysisProfileId_Slug",
                schema: "geek_seo",
                table: "site_analysis_profile_url_pattern_topics",
                columns: new[] { "SiteAnalysisProfileId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_analysis_profile_page_content_items",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profile_page_content_meta",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profile_site_crawl_meta",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profile_site_page_links",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profile_site_pages",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profile_url_pattern_topics",
                schema: "geek_seo");
        }
    }
}
