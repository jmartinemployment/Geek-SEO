using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUrlResearchTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_seo_content_documents_seo_url_research_UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropTable(
                name: "seo_url_research_closing_faq",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_competitor_heading",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_organic",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_paa",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_pasf",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_section_hint",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_source_heading",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_term",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research_competitor",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "seo_url_research",
                schema: "geek_seo");

            migrationBuilder.DropIndex(
                name: "IX_seo_content_documents_UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "seo_url_research",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessContext = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataQuality = table.Column<string>(type: "text", nullable: true),
                    DataQualityNotes = table.Column<string>(type: "text", nullable: true),
                    DerivedKeyword = table.Column<string>(type: "text", nullable: false),
                    DirectAnswerInstruction = table.Column<string>(type: "text", nullable: false),
                    DominantContentFormat = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    GbpSource = table.Column<string>(type: "text", nullable: false),
                    IntentJustification = table.Column<string>(type: "text", nullable: false),
                    IntentPrimary = table.Column<string>(type: "text", nullable: false),
                    MedianH2CountTop5 = table.Column<int>(type: "integer", nullable: false),
                    MedianTitleLengthTop10 = table.Column<int>(type: "integer", nullable: false),
                    MedianWordCountTop5 = table.Column<int>(type: "integer", nullable: false),
                    MustBeatPaf = table.Column<bool>(type: "boolean", nullable: false),
                    PafBeatStrategy = table.Column<string>(type: "text", nullable: false),
                    PafFormat = table.Column<string>(type: "text", nullable: false),
                    PafSourceUrl = table.Column<string>(type: "text", nullable: false),
                    PafText = table.Column<string>(type: "text", nullable: false),
                    PafType = table.Column<string>(type: "text", nullable: false),
                    ResearchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SearchLocation = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SupersedesResearchId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_seo_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_closing_faq",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_closing_faq", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_closing_faq_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_competitor",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedWordCount = table.Column<int>(type: "integer", nullable: false),
                    H1 = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_competitor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_competitor_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_organic",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Snippet = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_organic", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_organic_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_paa",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    SerpAnswerPreview = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_paa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_paa_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_pasf",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_pasf", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_pasf_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_section_hint",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Movement = table.Column<int>(type: "integer", nullable: false),
                    SubtopicsFromSerp = table.Column<string[]>(type: "text[]", nullable: false),
                    SuggestedH2 = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_section_hint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_section_hint_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_source_heading",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_source_heading", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_source_heading_seo_url_research_UrlResearc~",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_term",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UrlResearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Term = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_term", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_term_seo_url_research_UrlResearchId",
                        column: x => x.UrlResearchId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seo_url_research_competitor_heading",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CompetitorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_url_research_competitor_heading", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seo_url_research_competitor_heading_seo_url_research_compet~",
                        column: x => x.CompetitorId,
                        principalSchema: "geek_seo",
                        principalTable: "seo_url_research_competitor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_documents_UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_ProjectId",
                schema: "geek_seo",
                table: "seo_url_research",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_ProjectId_Status",
                schema: "geek_seo",
                table: "seo_url_research",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_SourceUrl",
                schema: "geek_seo",
                table: "seo_url_research",
                column: "SourceUrl");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_UserId",
                schema: "geek_seo",
                table: "seo_url_research",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_closing_faq_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_closing_faq",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_competitor_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_competitor",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_competitor_heading_CompetitorId",
                schema: "geek_seo",
                table: "seo_url_research_competitor_heading",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_organic_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_organic",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_paa_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_paa",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_pasf_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_pasf",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_section_hint_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_section_hint",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_source_heading_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_source_heading",
                column: "UrlResearchId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_url_research_term_UrlResearchId",
                schema: "geek_seo",
                table: "seo_url_research_term",
                column: "UrlResearchId");

            migrationBuilder.AddForeignKey(
                name: "FK_seo_content_documents_seo_url_research_UrlResearchId",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "UrlResearchId",
                principalSchema: "geek_seo",
                principalTable: "seo_url_research",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
