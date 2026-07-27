using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentWriterV3.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialContentWriterV3Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_writer_v3");

            migrationBuilder.CreateTable(
                name: "Workspaces",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalSchema: "content_writer_v3",
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientProfiles",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientProfiles_Clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "content_writer_v3",
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PainPoints",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ReaderSymptom = table.Column<string>(type: "text", nullable: false),
                    CostOfInaction = table.Column<string>(type: "text", nullable: false),
                    OfferTerminology = table.Column<string>(type: "text", nullable: false),
                    Objections = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    StaleSince = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PainPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PainPoints_Clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "content_writer_v3",
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientProfileVersions",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ApprovedFactsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProhibitedClaimsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientProfileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientProfileVersions_ClientProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "content_writer_v3",
                        principalTable: "ClientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentCampaigns",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Keyword = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentCampaigns_Clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "content_writer_v3",
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientBrandVoiceLinks",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandVoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientBrandVoiceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientBrandVoiceLinks_ClientProfileVersions_ProfileVersionId",
                        column: x => x.ProfileVersionId,
                        principalSchema: "content_writer_v3",
                        principalTable: "ClientProfileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentAssets",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAssets_ContentCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "content_writer_v3",
                        principalTable: "ContentCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LeaseOwner = table.Column<string>(type: "text", nullable: true),
                    LeaseExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequeuedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequeuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InputVersion = table.Column<int>(type: "integer", nullable: false),
                    OutputVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_ContentCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "content_writer_v3",
                        principalTable: "ContentCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentAssetVersions",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    BodyDocumentJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAssetVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAssetVersions_ContentAssets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "content_writer_v3",
                        principalTable: "ContentAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PainPointEvidenceLinks",
                schema: "content_writer_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PainPointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchEvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PainPointEvidenceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PainPointEvidenceLinks_PainPoints_PainPointId",
                        column: x => x.PainPointId,
                        principalSchema: "content_writer_v3",
                        principalTable: "PainPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Clients_WorkspaceId_Name",
                schema: "content_writer_v3",
                table: "Clients",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentCampaigns_ClientId_Name",
                schema: "content_writer_v3",
                table: "ContentCampaigns",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IdempotencyKey",
                schema: "content_writer_v3",
                table: "Jobs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PainPoints_ClientId_Name",
                schema: "content_writer_v3",
                table: "PainPoints",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssetVersions_AssetId",
                schema: "content_writer_v3",
                table: "ContentAssetVersions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientBrandVoiceLinks_ProfileVersionId",
                schema: "content_writer_v3",
                table: "ClientBrandVoiceLinks",
                column: "ProfileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfileVersions_ProfileId",
                schema: "content_writer_v3",
                table: "ClientProfileVersions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CampaignId",
                schema: "content_writer_v3",
                table: "Jobs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PainPointEvidenceLinks_PainPointId",
                schema: "content_writer_v3",
                table: "PainPointEvidenceLinks",
                column: "PainPointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSchema(
                name: "content_writer_v3");
        }
    }
}
