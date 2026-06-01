using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRankTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Clicks",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.DropColumn(
                name: "Ctr",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.DropColumn(
                name: "Impressions",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.AlterColumn<int>(
                name: "Position",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PageUrl",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "seo_tracked_keywords",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Keyword = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Device = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_tracked_keywords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seo_rank_tracking_ProjectId_Keyword_Date",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                columns: new[] { "ProjectId", "Keyword", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seo_tracked_keywords_ProjectId_Keyword",
                schema: "geek_seo",
                table: "seo_tracked_keywords",
                columns: new[] { "ProjectId", "Keyword" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_tracked_keywords",
                schema: "geek_seo");

            migrationBuilder.DropIndex(
                name: "IX_seo_rank_tracking_ProjectId_Keyword_Date",
                schema: "geek_seo",
                table: "seo_rank_tracking");

            migrationBuilder.AlterColumn<decimal>(
                name: "Position",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PageUrl",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Clicks",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Ctr",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Impressions",
                schema: "geek_seo",
                table: "seo_rank_tracking",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
