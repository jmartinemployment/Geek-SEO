using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordBundleToContentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "KeywordBundleCapturedAt",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeywordBundleJson",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteProfileId",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeywordBundleCapturedAt",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "KeywordBundleJson",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "SiteProfileId",
                schema: "geek_seo",
                table: "seo_content_documents");
        }
    }
}
