using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceListingExcerptWithPresentationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvertisingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.RenameColumn(
                name: "ListingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                newName: "PillarPageUseCaseExcerpt");

            migrationBuilder.AddColumn<string>(
                name: "Advertisement",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeUseCaseExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewspaperExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Advertisement",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "HeroExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "HomeUseCaseExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "NewspaperExcerpt",
                schema: "content_writer",
                table: "GeneratedContents");

            migrationBuilder.RenameColumn(
                name: "PillarPageUseCaseExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                newName: "ListingExcerpt");

            migrationBuilder.AddColumn<string>(
                name: "AdvertisingExcerpt",
                schema: "content_writer",
                table: "GeneratedContents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
