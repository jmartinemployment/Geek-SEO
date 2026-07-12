using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentFigureImageStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageRelativePath",
                schema: "content_writer",
                table: "ContentFigures",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageStorage",
                schema: "content_writer",
                table: "ContentFigures",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "site_static");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageRelativePath",
                schema: "content_writer",
                table: "ContentFigures");

            migrationBuilder.DropColumn(
                name: "ImageStorage",
                schema: "content_writer",
                table: "ContentFigures");
        }
    }
}
