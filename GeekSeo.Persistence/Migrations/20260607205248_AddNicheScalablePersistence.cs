using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNicheScalablePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnrichmentStatus",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersistStage",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScanChangeScore",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanFingerprint",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructureStatus",
                schema: "geek_seo",
                table: "niche_profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateId",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnrichedAt",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentStatus",
                schema: "geek_seo",
                table: "niche_pillars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "niche_topic_candidates",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NicheProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    ExclusionReason = table.Column<string>(type: "text", nullable: true),
                    DedicatedPageUrl = table.Column<string>(type: "text", nullable: true),
                    InternalLinkCount = table.Column<int>(type: "integer", nullable: false),
                    ContentDepthScore = table.Column<decimal>(type: "numeric", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niche_topic_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_niche_topic_candidates_niche_profiles_NicheProfileId",
                        column: x => x.NicheProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "niche_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_niche_topic_candidates_NicheProfileId",
                schema: "geek_seo",
                table: "niche_topic_candidates",
                column: "NicheProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_niche_topic_candidates_NicheProfileId_IsSelected",
                schema: "geek_seo",
                table: "niche_topic_candidates",
                columns: new[] { "NicheProfileId", "IsSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_niche_topic_candidates_NicheProfileId_Slug",
                schema: "geek_seo",
                table: "niche_topic_candidates",
                columns: new[] { "NicheProfileId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niche_topic_candidates",
                schema: "geek_seo");

            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "PersistStage",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "ScanChangeScore",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "ScanFingerprint",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "StructureStatus",
                schema: "geek_seo",
                table: "niche_profiles");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.DropColumn(
                name: "EnrichedAt",
                schema: "geek_seo",
                table: "niche_pillars");

            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                schema: "geek_seo",
                table: "niche_pillars");
        }
    }
}
