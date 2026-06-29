using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentDocumentClusterSlice2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishSlug",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpokeSourcePhrase",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpokeSourceType",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_documents_ProjectId_PublishSlug",
                schema: "geek_seo",
                table: "seo_content_documents",
                columns: new[] { "ProjectId", "PublishSlug" },
                unique: true,
                filter: "\"PublishSlug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_seo_content_documents_ProjectId_PublishSlug",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "PublishSlug",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "SpokeSourcePhrase",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "SpokeSourceType",
                schema: "geek_seo",
                table: "seo_content_documents");
        }
    }
}
