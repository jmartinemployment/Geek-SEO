using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_analysis_profiles",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    PrimaryFocus = table.Column<string>(type: "text", nullable: false),
                    FocusDescription = table.Column<string>(type: "text", nullable: false),
                    FocusTags = table.Column<string[]>(type: "text[]", nullable: false),
                    AudienceType = table.Column<string>(type: "text", nullable: false),
                    CompetitionLevel = table.Column<string>(type: "text", nullable: false),
                    DiscoveryMethod = table.Column<string>(type: "text", nullable: false),
                    TopicalAuthorityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPillarsIdentified = table.Column<int>(type: "integer", nullable: false),
                    PillarsCovered = table.Column<int>(type: "integer", nullable: false),
                    PillarsPartial = table.Column<int>(type: "integer", nullable: false),
                    PillarsGap = table.Column<int>(type: "integer", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAnalysisDue = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AnalysisVersion = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_competitors",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    SerpPresence = table.Column<int>(type: "integer", nullable: false),
                    EstimatedAuthorityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PillarsRanking = table.Column<int>(type: "integer", nullable: false),
                    StrengthAssessment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_competitors_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_entities",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    MentionFrequency = table.Column<int>(type: "integer", nullable: false),
                    PresentOnDomain = table.Column<bool>(type: "boolean", nullable: false),
                    AssociatedPillarIds = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_entities_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_pillars",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PillarTopic = table.Column<string>(type: "text", nullable: false),
                    PillarSlug = table.Column<string>(type: "text", nullable: false),
                    PrimaryKeyword = table.Column<string>(type: "text", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: true),
                    SearchIntent = table.Column<string>(type: "text", nullable: false),
                    SearchVolume = table.Column<int>(type: "integer", nullable: false),
                    KeywordDifficulty = table.Column<decimal>(type: "numeric", nullable: false),
                    CoverageStatus = table.Column<string>(type: "text", nullable: false),
                    CoverageScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ExistingPageCount = table.Column<int>(type: "integer", nullable: false),
                    RequiredSubtopicCount = table.Column<int>(type: "integer", nullable: false),
                    CoveredSubtopicCount = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StrategicPriority = table.Column<string>(type: "text", nullable: false),
                    ContentAngle = table.Column<string>(type: "text", nullable: true),
                    EstimatedTrafficPotential = table.Column<decimal>(type: "numeric", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_pillars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_pillars_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_pillar_pages",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PillarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    PageTitle = table.Column<string>(type: "text", nullable: true),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    CoverageQuality = table.Column<string>(type: "text", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    TopicsFound = table.Column<string[]>(type: "text[]", nullable: false),
                    GapsFound = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_pillar_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_pillar_pages_site_analysis_pillars_PillarId",
                        column: x => x.PillarId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_pillars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_analysis_subtopics",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PillarId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubtopicTitle = table.Column<string>(type: "text", nullable: false),
                    TargetKeyword = table.Column<string>(type: "text", nullable: false),
                    SearchIntent = table.Column<string>(type: "text", nullable: false),
                    SearchVolume = table.Column<int>(type: "integer", nullable: false),
                    KeywordDifficulty = table.Column<decimal>(type: "numeric", nullable: false),
                    CoverageStatus = table.Column<string>(type: "text", nullable: false),
                    ExistingUrl = table.Column<string>(type: "text", nullable: true),
                    RecommendedFormat = table.Column<string>(type: "text", nullable: false),
                    RecommendedWordCount = table.Column<int>(type: "integer", nullable: false),
                    FixEffort = table.Column<string>(type: "text", nullable: false),
                    IsQuickWin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_analysis_subtopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_analysis_subtopics_site_analysis_pillars_PillarId",
                        column: x => x.PillarId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_pillars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_competitors_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_competitors",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_entities_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_entities",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_pillar_pages_PillarId",
                schema: "geek_seo",
                table: "site_analysis_pillar_pages",
                column: "PillarId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_pillars_CoverageStatus",
                schema: "geek_seo",
                table: "site_analysis_pillars",
                column: "CoverageStatus");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_pillars_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "site_analysis_pillars",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profiles_Domain",
                schema: "geek_seo",
                table: "site_analysis_profiles",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profiles_ProjectId",
                schema: "geek_seo",
                table: "site_analysis_profiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_profiles_Status",
                schema: "geek_seo",
                table: "site_analysis_profiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_subtopics_IsQuickWin",
                schema: "geek_seo",
                table: "site_analysis_subtopics",
                column: "IsQuickWin");

            migrationBuilder.CreateIndex(
                name: "IX_site_analysis_subtopics_PillarId",
                schema: "geek_seo",
                table: "site_analysis_subtopics",
                column: "PillarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_analysis_competitors",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_entities",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_pillar_pages",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_subtopics",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_pillars",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "site_analysis_profiles",
                schema: "geek_seo");
        }
    }
}
