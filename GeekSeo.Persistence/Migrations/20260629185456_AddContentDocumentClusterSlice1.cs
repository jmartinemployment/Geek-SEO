using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentDocumentClusterSlice1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentKind",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "standalone");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_documents_ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_seo_content_documents_ProjectId_DocumentKind",
                schema: "geek_seo",
                table: "seo_content_documents",
                columns: new[] { "ProjectId", "DocumentKind" });

            migrationBuilder.AddForeignKey(
                name: "FK_seo_content_documents_seo_content_documents_ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents",
                column: "ParentDocumentId",
                principalSchema: "geek_seo",
                principalTable: "seo_content_documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_seo_content_documents_seo_content_documents_ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropIndex(
                name: "IX_seo_content_documents_ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropIndex(
                name: "IX_seo_content_documents_ProjectId_DocumentKind",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "DocumentKind",
                schema: "geek_seo",
                table: "seo_content_documents");

            migrationBuilder.DropColumn(
                name: "ParentDocumentId",
                schema: "geek_seo",
                table: "seo_content_documents");
        }
    }
}
