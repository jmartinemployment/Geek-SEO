using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SiteAnalyzer2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_sa2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sa2");

            migrationBuilder.CreateTable(
                name: "crawl_priority_url_patterns",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Pattern = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crawl_priority_url_patterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "known_platform_domains",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_known_platform_domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MaxCrawlDepth = table.Column<int>(type: "integer", nullable: false),
                    MaxCrawlPages = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reference_exclude_domains",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_exclude_domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analysis_runs",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Keyword = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TargetSiteUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SerpProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IncludeReferenceDomains = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SerpClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerpMaxPage = table.Column<int>(type: "integer", nullable: false),
                    SerpLocationCode = table.Column<int>(type: "integer", nullable: true),
                    SerpLanguageCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SerpDevice = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SerpOs = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SerpDepth = table.Column<int>(type: "integer", nullable: false),
                    SerpSeDomain = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SerpCheckUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SerpCapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SerpSpellJson = table.Column<string>(type: "text", nullable: true),
                    SerpRefinementChipsJson = table.Column<string>(type: "text", nullable: true),
                    SerpItemTypesJson = table.Column<string>(type: "text", nullable: true),
                    SerpSeResultsCount = table.Column<long>(type: "bigint", nullable: true),
                    SerpPagesCount = table.Column<int>(type: "integer", nullable: false),
                    SerpItemsCount = table.Column<int>(type: "integer", nullable: false),
                    SerpLocalPackPresent = table.Column<bool>(type: "boolean", nullable: false),
                    SerpShoppingResultsPresent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analysis_runs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "sa2",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_seed_domains",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_seed_domains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitor_seed_domains_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "sa2",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_owned_domains",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_owned_domains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_owned_domains_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "sa2",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_profiles",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    GeekSeoProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BusinessType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BusinessDescription = table.Column<string>(type: "text", nullable: true),
                    GeneratedSchemaJson = table.Column<string>(type: "text", nullable: true),
                    BusinessProfileAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_site_profiles_projects_GeekSeoProjectId",
                        column: x => x.GeekSeoProjectId,
                        principalSchema: "sa2",
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comparison_checks",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comparison_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comparison_checks_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_pages",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FetchMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    HttpStatus = table.Column<int>(type: "integer", nullable: false),
                    DepthFromSeed = table.Column<int>(type: "integer", nullable: true),
                    SeedRankAbsolute = table.Column<int>(type: "integer", nullable: false),
                    CrawledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitor_pages_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "findings",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_findings_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    CanonicalUrl = table.Column<string>(type: "text", nullable: true),
                    FetchMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    HttpStatus = table.Column<int>(type: "integer", nullable: false),
                    IsTargetSite = table.Column<bool>(type: "boolean", nullable: false),
                    DepthFromHomepage = table.Column<int>(type: "integer", nullable: true),
                    HtmlContent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pages_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "run_gates",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationMessage = table.Column<string>(type: "text", nullable: false),
                    RowCountsJson = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_gates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_run_gates_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serp_items",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RankGroup = table.Column<int>(type: "integer", nullable: false),
                    RankAbsolute = table.Column<int>(type: "integer", nullable: false),
                    Page = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Xpath = table.Column<string>(type: "text", nullable: true),
                    RectangleJson = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CacheUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RelatedSearchUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Breadcrumb = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    WebsiteName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsImage = table.Column<bool>(type: "boolean", nullable: false),
                    IsVideo = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeaturedSnippet = table.Column<bool>(type: "boolean", nullable: false),
                    IsMalicious = table.Column<bool>(type: "boolean", nullable: false),
                    IsWebStory = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PreSnippet = table.Column<string>(type: "text", nullable: true),
                    ExtendedSnippet = table.Column<string>(type: "text", nullable: true),
                    ImagesJson = table.Column<string>(type: "text", nullable: true),
                    AmpVersion = table.Column<bool>(type: "boolean", nullable: false),
                    RatingJson = table.Column<string>(type: "text", nullable: true),
                    PriceJson = table.Column<string>(type: "text", nullable: true),
                    FaqJson = table.Column<string>(type: "text", nullable: true),
                    ExtendedPeopleAlsoSearchJson = table.Column<string>(type: "text", nullable: true),
                    AboutThisResultJson = table.Column<string>(type: "text", nullable: true),
                    RelatedResultJson = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AiOverviewAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    AiOverviewMarkdown = table.Column<string>(type: "text", nullable: true),
                    AiOverviewStatusMessage = table.Column<string>(type: "text", nullable: true),
                    Ads = table.Column<bool>(type: "boolean", nullable: false),
                    Filtered = table.Column<bool>(type: "boolean", nullable: false),
                    FilterStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IncludeReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExcludeReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serp_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_serp_items_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "target_site_business_profiles",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetSiteUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    BusinessType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PrimaryServicesJson = table.Column<string>(type: "text", nullable: false),
                    ServiceArea = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GeneratedSchemaJson = table.Column<string>(type: "text", nullable: false),
                    HasExistingSchema = table.Column<bool>(type: "boolean", nullable: false),
                    ExistingSchemaMatches = table.Column<bool>(type: "boolean", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReusedFromRunId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_site_business_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_target_site_business_profiles_analysis_runs_RunId",
                        column: x => x.RunId,
                        principalSchema: "sa2",
                        principalTable: "analysis_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_page_headings",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitorPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_page_headings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitor_page_headings_competitor_pages_CompetitorPageId",
                        column: x => x.CompetitorPageId,
                        principalSchema: "sa2",
                        principalTable: "competitor_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_page_json_ld",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitorPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    ParsedType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_page_json_ld", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitor_page_json_ld_competitor_pages_CompetitorPageId",
                        column: x => x.CompetitorPageId,
                        principalSchema: "sa2",
                        principalTable: "competitor_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_page_meta_tags",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitorPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameOrProperty = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_page_meta_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competitor_page_meta_tags_competitor_pages_CompetitorPageId",
                        column: x => x.CompetitorPageId,
                        principalSchema: "sa2",
                        principalTable: "competitor_pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cross_run_links",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInternalToDomain = table.Column<bool>(type: "boolean", nullable: false),
                    Href = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cross_run_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cross_run_links_pages_FromPageId",
                        column: x => x.FromPageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cross_run_links_pages_ToPageId",
                        column: x => x.ToPageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "internal_links",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Href = table.Column<string>(type: "text", nullable: false),
                    AnchorText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_internal_links_pages_FromPageId",
                        column: x => x.FromPageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_internal_links_pages_ToPageId",
                        column: x => x.ToPageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_content_blocks",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_content_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_content_blocks_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_headings",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_headings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_headings_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_json_ld",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    ParsedType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_json_ld", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_json_ld_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_meta_tags",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameOrProperty = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_meta_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_meta_tags_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_rank_scores",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    GraphScope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_rank_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_page_rank_scores_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "sa2",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serp_item_highlighted",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerpItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serp_item_highlighted", x => x.Id);
                    table.ForeignKey(
                        name: "FK_serp_item_highlighted_serp_items_SerpItemId",
                        column: x => x.SerpItemId,
                        principalSchema: "sa2",
                        principalTable: "serp_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serp_item_links",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerpItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serp_item_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_serp_item_links_serp_items_SerpItemId",
                        column: x => x.SerpItemId,
                        principalSchema: "sa2",
                        principalTable: "serp_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "serp_related_queries",
                schema: "sa2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerpItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    QueryText = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    QueryType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_serp_related_queries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_serp_related_queries_serp_items_SerpItemId",
                        column: x => x.SerpItemId,
                        principalSchema: "sa2",
                        principalTable: "serp_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_runs_ProjectId",
                schema: "sa2",
                table: "analysis_runs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_comparison_checks_RunId",
                schema: "sa2",
                table: "comparison_checks",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_competitor_page_headings_CompetitorPageId",
                schema: "sa2",
                table: "competitor_page_headings",
                column: "CompetitorPageId");

            migrationBuilder.CreateIndex(
                name: "IX_competitor_page_json_ld_CompetitorPageId",
                schema: "sa2",
                table: "competitor_page_json_ld",
                column: "CompetitorPageId");

            migrationBuilder.CreateIndex(
                name: "IX_competitor_page_meta_tags_CompetitorPageId",
                schema: "sa2",
                table: "competitor_page_meta_tags",
                column: "CompetitorPageId");

            migrationBuilder.CreateIndex(
                name: "IX_competitor_pages_RunId",
                schema: "sa2",
                table: "competitor_pages",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_competitor_pages_RunId_Domain",
                schema: "sa2",
                table: "competitor_pages",
                columns: new[] { "RunId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_seed_domains_ProjectId",
                schema: "sa2",
                table: "competitor_seed_domains",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_crawl_priority_url_patterns_Pattern",
                schema: "sa2",
                table: "crawl_priority_url_patterns",
                column: "Pattern",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cross_run_links_FromPageId",
                schema: "sa2",
                table: "cross_run_links",
                column: "FromPageId");

            migrationBuilder.CreateIndex(
                name: "IX_cross_run_links_RunId",
                schema: "sa2",
                table: "cross_run_links",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_cross_run_links_ToPageId",
                schema: "sa2",
                table: "cross_run_links",
                column: "ToPageId");

            migrationBuilder.CreateIndex(
                name: "IX_findings_RunId",
                schema: "sa2",
                table: "findings",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_links_FromPageId",
                schema: "sa2",
                table: "internal_links",
                column: "FromPageId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_links_RunId",
                schema: "sa2",
                table: "internal_links",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_links_ToPageId",
                schema: "sa2",
                table: "internal_links",
                column: "ToPageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_content_blocks_PageId",
                schema: "sa2",
                table: "page_content_blocks",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_headings_PageId",
                schema: "sa2",
                table: "page_headings",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_json_ld_PageId",
                schema: "sa2",
                table: "page_json_ld",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_meta_tags_PageId",
                schema: "sa2",
                table: "page_meta_tags",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_rank_scores_PageId",
                schema: "sa2",
                table: "page_rank_scores",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_page_rank_scores_RunId",
                schema: "sa2",
                table: "page_rank_scores",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_pages_ProjectId",
                schema: "sa2",
                table: "pages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_pages_RunId",
                schema: "sa2",
                table: "pages",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_project_owned_domains_ProjectId",
                schema: "sa2",
                table: "project_owned_domains",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_run_gates_RunId",
                schema: "sa2",
                table: "run_gates",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_serp_item_highlighted_SerpItemId",
                schema: "sa2",
                table: "serp_item_highlighted",
                column: "SerpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_serp_item_links_SerpItemId",
                schema: "sa2",
                table: "serp_item_links",
                column: "SerpItemId");

            migrationBuilder.CreateIndex(
                name: "IX_serp_items_ProjectId",
                schema: "sa2",
                table: "serp_items",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_serp_items_RunId_RankAbsolute",
                schema: "sa2",
                table: "serp_items",
                columns: new[] { "RunId", "RankAbsolute" });

            migrationBuilder.CreateIndex(
                name: "IX_serp_items_RunId_Type",
                schema: "sa2",
                table: "serp_items",
                columns: new[] { "RunId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_serp_related_queries_SerpItemId_Sequence",
                schema: "sa2",
                table: "serp_related_queries",
                columns: new[] { "SerpItemId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_site_profiles_GeekSeoProjectId",
                schema: "sa2",
                table: "site_profiles",
                column: "GeekSeoProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_site_profiles_SiteUrl",
                schema: "sa2",
                table: "site_profiles",
                column: "SiteUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_target_site_business_profiles_ProjectId_TargetSiteUrl",
                schema: "sa2",
                table: "target_site_business_profiles",
                columns: new[] { "ProjectId", "TargetSiteUrl" });

            migrationBuilder.CreateIndex(
                name: "IX_target_site_business_profiles_RunId",
                schema: "sa2",
                table: "target_site_business_profiles",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comparison_checks",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "competitor_page_headings",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "competitor_page_json_ld",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "competitor_page_meta_tags",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "competitor_seed_domains",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "crawl_priority_url_patterns",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "cross_run_links",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "findings",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "internal_links",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "known_platform_domains",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "page_content_blocks",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "page_headings",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "page_json_ld",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "page_meta_tags",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "page_rank_scores",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "project_owned_domains",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "reference_exclude_domains",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "run_gates",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "serp_item_highlighted",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "serp_item_links",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "serp_related_queries",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "site_profiles",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "target_site_business_profiles",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "competitor_pages",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "serp_items",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "analysis_runs",
                schema: "sa2");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "sa2");
        }
    }
}
