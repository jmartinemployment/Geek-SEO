using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSerpRankSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "serp_rank_snapshots",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportSequence = table.Column<int>(type: "integer", nullable: false),
                    SerpCapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetOrganicPosition = table.Column<int>(type: "integer", nullable: true),
                    TargetOrganicUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OrganicResultCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serp_rank_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_serp_rank_snapshots_ProjectId",
                schema: "sa2",
                table: "serp_rank_snapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_serp_rank_snapshots_RunId_ImportSequence",
                schema: "sa2",
                table: "serp_rank_snapshots",
                columns: new[] { "RunId", "ImportSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "serp_rank_snapshots",
                schema: "sa2");
        }
    }
}
