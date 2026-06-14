using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNicheProfilePhase2RelationalStepTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "niche_profile_page_content_items",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    ItemKind = table.Column<string>(type: "text", nullable: false),
                    ItemText = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_page_content_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_page_content_items_niche_profiles_NicheProfil~",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_page_content_meta",
                schema: "geek_seo",
                columns: table => new
                {
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    ListItemsScanned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_page_content_meta", x => x.NicheProfileId);
                    table.ForeignKey(
                        name: "FK_niche_profile_page_content_meta_niche_profiles_NicheProfile~",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_site_crawl_meta",
                schema: "geek_seo",
                columns: table => new
                {
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PagesAttempted = table.Column<int>(type: "integer", nullable: false),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_site_crawl_meta", x => x.NicheProfileId);
                    table.ForeignKey(
                        name: "FK_niche_profile_site_crawl_meta_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_site_page_links",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    TargetUrl = table.Column<string>(type: "text", nullable: false),
                    AnchorText = table.Column<string>(type: "text", nullable: false),
                    InferredFromUrlSlug = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_site_page_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_site_page_links_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_site_pages",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FetchMethod = table.Column<string>(type: "text", nullable: false),
                    VisibleText = table.Column<string>(type: "text", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_site_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_site_pages_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_url_pattern_topics",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    PathSegment = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_url_pattern_topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_url_pattern_topics_niche_profiles_NicheProfil~",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_page_content_items_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_page_content_items",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_site_page_links_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_site_page_links",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_site_pages_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_site_pages",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_site_pages_NicheProfileId_Url",
                schema: "geek_seo",
                table: "niche_profile_site_pages",
                columns: new[] { "NicheProfileId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_url_pattern_topics_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_url_pattern_topics",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_url_pattern_topics_NicheProfileId_Slug",
                schema: "geek_seo",
                table: "niche_profile_url_pattern_topics",
                columns: new[] { "NicheProfileId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niche_profile_page_content_items",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_page_content_meta",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_site_crawl_meta",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_site_page_links",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_site_pages",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_url_pattern_topics",
                schema: "geek_seo");
        }
    }
}
