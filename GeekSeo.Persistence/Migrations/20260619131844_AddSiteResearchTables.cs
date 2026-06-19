using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteResearchTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seo_site_research",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteUrl = table.Column<string>(type: "text", nullable: false),
                    DiscoveredUrlsJson = table.Column<string>(type: "jsonb", nullable: false),
                    BusinessSummary = table.Column<string>(type: "text", nullable: false),
                    InternalLinkMapJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_site_research", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_site_research_seo_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_site_analyzer_step_run",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteResearchId = table.Column<Guid>(type: "uuid", nullable: true),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Log = table.Column<string>(type: "text", nullable: false),
                    CountsJson = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_site_analyzer_step_run", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_site_analyzer_step_run_seo_site_research_SiteResearchId",
                        column: x => x.SiteResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_site_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_seo_site_analyzer_step_run_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_site_research_page",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Html = table.Column<string>(type: "text", nullable: false),
                    HeadingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    JsonLdJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExtractSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ExtractError = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_site_research_page", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_site_research_page_seo_site_research_SiteResearchId",
                        column: x => x.SiteResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_site_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_analyzer_step_run_SiteResearchId_StepNumber",
                schema: "geek_seo",
                table: "seo_site_analyzer_step_run",
                columns: new[] { "SiteResearchId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_analyzer_step_run_UrlResearchId_StepNumber",
                schema: "geek_seo",
                table: "seo_site_analyzer_step_run",
                columns: new[] { "UrlResearchId", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_research_ProjectId",
                schema: "geek_seo",
                table: "seo_site_research",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_research_UserId",
                schema: "geek_seo",
                table: "seo_site_research",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_research_page_SiteResearchId",
                schema: "geek_seo",
                table: "seo_site_research_page",
                column: "SiteResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_site_research_page_SiteResearchId_Url",
                schema: "geek_seo",
                table: "seo_site_research_page",
                columns: new[] { "SiteResearchId", "Url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_site_analyzer_step_run",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_site_research_page",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_site_research",
                schema: "geek_seo");
        }
    }
}
