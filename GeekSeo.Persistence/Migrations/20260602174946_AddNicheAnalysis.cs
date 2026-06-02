using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNicheAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "niche_profiles",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    PrimaryNiche = table.Column<string>(type: "text", nullable: false),
                    NicheDescription = table.Column<string>(type: "text", nullable: false),
                    NicheTags = table.Column<string[]>(type: "text[]", nullable: false),
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
                    table.PrimaryKey("PK_niche_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "niche_competitors",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    SerpPresence = table.Column<int>(type: "integer", nullable: false),
                    EstimatedAuthorityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    PillarsRanking = table.Column<int>(type: "integer", nullable: false),
                    StrengthAssessment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_competitors_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_entities",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    MentionFrequency = table.Column<int>(type: "integer", nullable: false),
                    PresentOnDomain = table.Column<bool>(type: "boolean", nullable: false),
                    AssociatedPillarIds = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_entities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_entities_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_pillars",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_niche_pillars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_pillars_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_pillar_pages",
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
                    table.PrimaryKey("PK_niche_pillar_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_pillar_pages_niche_pillars_PillarId",
                        column: x => x.PillarId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_pillars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_subtopics",
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
                    table.PrimaryKey("PK_niche_subtopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_subtopics_niche_pillars_PillarId",
                        column: x => x.PillarId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_pillars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_niche_competitors_NicheProfileId",
                schema: "geek_seo",
                table: "niche_competitors",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_entities_NicheProfileId",
                schema: "geek_seo",
                table: "niche_entities",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_pillar_pages_PillarId",
                schema: "geek_seo",
                table: "niche_pillar_pages",
                column: "PillarId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_pillars_CoverageStatus",
                schema: "geek_seo",
                table: "niche_pillars",
                column: "CoverageStatus");

            migrationBuilder.CreateIndex(
                name: "IX_niche_pillars_NicheProfileId",
                schema: "geek_seo",
                table: "niche_pillars",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profiles_Domain",
                schema: "geek_seo",
                table: "niche_profiles",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profiles_ProjectId",
                schema: "geek_seo",
                table: "niche_profiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profiles_Status",
                schema: "geek_seo",
                table: "niche_profiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_niche_subtopics_IsQuickWin",
                schema: "geek_seo",
                table: "niche_subtopics",
                column: "IsQuickWin");

            migrationBuilder.CreateIndex(
                name: "IX_niche_subtopics_PillarId",
                schema: "geek_seo",
                table: "niche_subtopics",
                column: "PillarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niche_competitors",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_entities",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_pillar_pages",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_subtopics",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_pillars",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profiles",
                schema: "geek_seo");
        }
    }
}
