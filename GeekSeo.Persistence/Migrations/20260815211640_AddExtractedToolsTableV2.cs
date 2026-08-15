using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractedToolsTableV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedTools_site_analysis_profiles_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "ExtractedTools");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExtractedTools",
                schema: "geek_seo",
                table: "ExtractedTools");

            migrationBuilder.RenameTable(
                name: "ExtractedTools",
                schema: "geek_seo",
                newName: "extracted_tools",
                newSchema: "geek_seo");

            migrationBuilder.RenameIndex(
                name: "IX_ExtractedTools_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "extracted_tools",
                newName: "IX_extracted_tools_SiteAnalysisProfileId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "geek_seo",
                table: "extracted_tools",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_extracted_tools",
                schema: "geek_seo",
                table: "extracted_tools",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools",
                columns: new[] { "Name", "Department", "Body" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extracted_tools_SitePageId",
                schema: "geek_seo",
                table: "extracted_tools",
                column: "SitePageId");

            migrationBuilder.AddForeignKey(
                name: "FK_extracted_tools_site_analysis_profiles_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "extracted_tools",
                column: "SiteAnalysisProfileId",
                principalSchema: "geek_seo",
                principalTable: "site_analysis_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_extracted_tools_site_analysis_profiles_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.DropPrimaryKey(
                name: "PK_extracted_tools",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.DropIndex(
                name: "IX_extracted_tools_Name_Department_Body",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.DropIndex(
                name: "IX_extracted_tools_SitePageId",
                schema: "geek_seo",
                table: "extracted_tools");

            migrationBuilder.RenameTable(
                name: "extracted_tools",
                schema: "geek_seo",
                newName: "ExtractedTools",
                newSchema: "geek_seo");

            migrationBuilder.RenameIndex(
                name: "IX_extracted_tools_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "ExtractedTools",
                newName: "IX_ExtractedTools_SiteAnalysisProfileId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "geek_seo",
                table: "ExtractedTools",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExtractedTools",
                schema: "geek_seo",
                table: "ExtractedTools",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedTools_site_analysis_profiles_SiteAnalysisProfileId",
                schema: "geek_seo",
                table: "ExtractedTools",
                column: "SiteAnalysisProfileId",
                principalSchema: "geek_seo",
                principalTable: "site_analysis_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
