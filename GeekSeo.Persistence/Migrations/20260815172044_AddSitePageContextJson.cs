using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSitePageContextJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Canonical",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContextJson",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FetchedAt",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

            migrationBuilder.AddColumn<string>(
                name: "FinalUrl",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NoFollow",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoIndex",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RedirectChainJson",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Canonical",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "ContextJson",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "FetchedAt",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "FinalUrl",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "NoFollow",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "NoIndex",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "RedirectChainJson",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                schema: "geek_seo",
                table: "site_analysis_profile_site_pages");
        }
    }
}
