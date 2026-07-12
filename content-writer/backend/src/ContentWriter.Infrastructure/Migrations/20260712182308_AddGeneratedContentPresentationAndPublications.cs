using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedContentPresentationAndPublications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvertisingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayTitle",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroImageUrl",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceAppName",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceAppOrder",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectPublications",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    GeekPostId = table.Column<int>(type: "integer", nullable: false),
                    GeekApiSlug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPublications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "content_writer",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPublications_ProjectId_ContentType_GeekApiSlug",
                schema: "content_writer",
                table: "ProjectPublications",
                columns: new[] { "ProjectId", "ContentType", "GeekApiSlug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectPublications",
                schema: "content_writer");

            migrationBuilder.DropColumn(
                name: "AdvertisingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "DisplayTitle",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "HeroImageUrl",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "ListingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "SourceAppName",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "SourceAppOrder",
                schema: "content_writer",
                table: "GeneratedContents");
        }
    }
}
