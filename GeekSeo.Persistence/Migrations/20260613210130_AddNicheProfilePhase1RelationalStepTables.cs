using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNicheProfilePhase1RelationalStepTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "niche_profile_discovered_urls",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_discovered_urls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_discovered_urls_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_headings",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    HeadingLevel = table.Column<int>(type: "integer", nullable: false),
                    HeadingText = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_headings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_headings_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_navigation_links",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    LinkUrl = table.Column<string>(type: "text", nullable: false),
                    AnchorText = table.Column<string>(type: "text", nullable: true),
                    LinkArea = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_navigation_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_navigation_links_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_schema_signals",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaType = table.Column<string>(type: "text", nullable: false),
                    PropertyName = table.Column<string>(type: "text", nullable: false),
                    PropertyValue = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_schema_signals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_schema_signals_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_profile_step_runs",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    StepSlug = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    InputVersion = table.Column<int>(type: "integer", nullable: false),
                    OutputVersion = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_profile_step_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_profile_step_runs_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "niche_topic_candidate_evidence",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TopicCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceType = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    SourceLabel = table.Column<string>(type: "text", nullable: true),
                    EvidenceText = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_topic_candidate_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_topic_candidate_evidence_niche_topic_candidates_Topic~",
                        column: x => x.TopicCandidateId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_topic_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_discovered_urls_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_discovered_urls",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_discovered_urls_NicheProfileId_Url_SourceType",
                schema: "geek_seo",
                table: "niche_profile_discovered_urls",
                columns: new[] { "NicheProfileId", "Url", "SourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_headings_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_headings",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_navigation_links_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_navigation_links",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_schema_signals_NicheProfileId",
                schema: "geek_seo",
                table: "niche_profile_schema_signals",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_step_runs_NicheProfileId_StepNumber",
                schema: "geek_seo",
                table: "niche_profile_step_runs",
                columns: new[] { "NicheProfileId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_niche_profile_step_runs_NicheProfileId_StepSlug",
                schema: "geek_seo",
                table: "niche_profile_step_runs",
                columns: new[] { "NicheProfileId", "StepSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_niche_topic_candidate_evidence_TopicCandidateId",
                schema: "geek_seo",
                table: "niche_topic_candidate_evidence",
                column: "TopicCandidateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niche_profile_discovered_urls",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_headings",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_navigation_links",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_schema_signals",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_profile_step_runs",
                schema: "geek_seo");

            migrationBuilder.DropTable(
                name: "niche_topic_candidate_evidence",
                schema: "geek_seo");
        }
    }
}
