using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RescopeExtractedToolsUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_extracted_tools_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_SiteAnalysisProfileId_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools",
                columns: new[] { "SiteAnalysisProfileId", "Name", "Department", "Body" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_extracted_tools_SiteAnalysisProfileId_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools",
                columns: new[] { "Name", "Department", "Body" },
                unique: true);
        }
    }
}
