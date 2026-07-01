using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeekSeo.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGtmAccountConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seo_gtm_account_connections",
                schema: "geek_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GoogleEmail = table.Column<string>(type: "text", nullable: true),
                    EncryptedRefreshToken = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptionIv = table.Column<byte[]>(type: "bytea", nullable: false),
                    EncryptionTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seo_gtm_account_connections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_seo_gtm_account_connections_UserId_AccountKey",
                schema: "geek_seo",
                table: "seo_gtm_account_connections",
                columns: new[] { "UserId", "AccountKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_gtm_account_connections",
                schema: "geek_seo");
        }
    }
}
