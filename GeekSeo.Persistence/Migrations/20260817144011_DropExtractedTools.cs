using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropExtractedTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extracted_tools",
                schema: "geek_seo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "extracted_tools",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteAnalysisProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    ExtractedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Href = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SitePageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extracted_tools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_extracted_tools_site_analysis_profiles_SiteAnalysisProfileId",
                        column: x => x.SiteAnalysisProfileId,
                        principalSchema: "geek_seo",
                        principalTable: "site_analysis_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "extracted_tools",
                column: "SiteAnalysisProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_SiteAnalysisProfileId_SitePageId_Name_Depar~",
                schema: "geek_seo",
                table: "extracted_tools",
                columns: new[] { "SiteAnalysisProfileId", "SitePageId", "Name", "Department", "Body" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_SitePageId",
                schema: "geek_seo",
                table: "extracted_tools",
                column: "SitePageId");
        }
    }
}
