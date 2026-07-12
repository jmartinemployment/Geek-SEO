using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectToolsOutcomeAndWidenFigureSourceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolsGenerationOutcome",
                schema: "content_writer",
                table: "Projects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceType",
                schema: "content_writer",
                table: "ContentFigures",
                type: "character varying(576)",
                maxLength: 576,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolsGenerationOutcome",
                schema: "content_writer",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "SourceType",
                schema: "content_writer",
                table: "ContentFigures",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(576)",
                oldMaxLength: 576);
        }
    }
}
