using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialContentWriterSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_writer");

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProjectUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    TargetKeyword = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreferredProvider = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrawledSites",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    SiteName = table.Column<string>(type: "text", nullable: false),
                    JsonLdBlocks = table.Column<string>(type: "text", nullable: false),
                    Headings = table.Column<string>(type: "text", nullable: false),
                    Paragraphs = table.Column<string>(type: "text", nullable: false),
                    DetectedTone = table.Column<string>(type: "text", nullable: false),
                    DetectedFocus = table.Column<string>(type: "text", nullable: false),
                    PagesCrawled = table.Column<int>(type: "integer", nullable: false),
                    CrawledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawledSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawledSites_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "content_writer",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedContents",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Slug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    MetaDescription = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<string>(type: "text", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    SectionOutline = table.Column<string>(type: "text", nullable: false),
                    JsonLdSchema = table.Column<string>(type: "text", nullable: true),
                    RelatedArticleUrl = table.Column<string>(type: "text", nullable: true),
                    GeneratedByProvider = table.Column<int>(type: "integer", nullable: false),
                    GeneratedByModel = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedContents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "content_writer",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeywordSources",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    ExtractedTitle = table.Column<string>(type: "text", nullable: true),
                    ExtractedHeadings = table.Column<string>(type: "text", nullable: false),
                    ExtractedParagraphs = table.Column<string>(type: "text", nullable: false),
                    ExtractedQuestions = table.Column<string>(type: "text", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeywordSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "content_writer",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrawledSites_ProjectId",
                schema: "content_writer",
                table: "CrawledSites",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedContents_ProjectId",
                schema: "content_writer",
                table: "GeneratedContents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordSources_ProjectId",
                schema: "content_writer",
                table: "KeywordSources",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawledSites",
                schema: "content_writer");

            migrationBuilder.DropTable(
                name: "GeneratedContents",
                schema: "content_writer");

            migrationBuilder.DropTable(
                name: "KeywordSources",
                schema: "content_writer");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "content_writer");
        }
    }
}
