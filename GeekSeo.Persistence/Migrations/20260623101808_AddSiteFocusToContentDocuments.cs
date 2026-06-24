using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteFocusToContentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SiteFocusCapturedAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteFocusJson",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteFocusCapturedAt",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "SiteFocusJson",
                schema: "geek_seo",
                table: "seo_content_documents");
        }
    }
}
