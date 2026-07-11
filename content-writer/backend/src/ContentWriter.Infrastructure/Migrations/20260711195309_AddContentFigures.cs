using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentFigures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentFigures",
                schema: "content_writer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImagePromptContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SectionOrder = table.Column<int>(type: "integer", nullable: false),
                    HeadingSlug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Heading = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BriefText = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SkipReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ImageWidth = table.Column<int>(type: "integer", nullable: true),
                    ImageHeight = table.Column<int>(type: "integer", nullable: true),
                    ImageAlt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    GeekApiSlug = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GeekPostId = table.Column<int>(type: "integer", nullable: true),
                    NeedsFigureMerge = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentFigures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentFigures_GeneratedContents_ImagePromptContentId",
                        column: x => x.ImagePromptContentId,
                        principalSchema: "content_writer",
                        principalTable: "GeneratedContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContentFigures_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "content_writer",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentFigures_ImagePromptContentId",
                schema: "content_writer",
                table: "ContentFigures",
                column: "ImagePromptContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentFigures_ProjectId_SourceType_HeadingSlug",
                schema: "content_writer",
                table: "ContentFigures",
                columns: new[] { "ProjectId", "SourceType", "HeadingSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentFigures_ProjectId_SourceType_SectionOrder",
                schema: "content_writer",
                table: "ContentFigures",
                columns: new[] { "ProjectId", "SourceType", "SectionOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentFigures",
                schema: "content_writer");
        }
    }
}
