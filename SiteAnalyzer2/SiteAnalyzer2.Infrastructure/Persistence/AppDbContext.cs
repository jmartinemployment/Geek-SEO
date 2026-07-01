using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<SiteProfile> SiteProfiles => Set<SiteProfile>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<SerpItem> SerpItems => Set<SerpItem>();
    public DbSet<SerpItemLink> SerpItemLinks => Set<SerpItemLink>();
    public DbSet<SerpItemHighlighted> SerpItemHighlighted => Set<SerpItemHighlighted>();
    public DbSet<SerpRelatedQuery> SerpRelatedQueries => Set<SerpRelatedQuery>();
    public DbSet<ReferenceExcludeDomain> ReferenceExcludeDomains => Set<ReferenceExcludeDomain>();
    public DbSet<KnownPlatformDomain> KnownPlatformDomains => Set<KnownPlatformDomain>();
    public DbSet<ProjectOwnedDomain> ProjectOwnedDomains => Set<ProjectOwnedDomain>();
    public DbSet<CompetitorSeedDomain> CompetitorSeedDomains => Set<CompetitorSeedDomain>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageHeading> PageHeadings => Set<PageHeading>();
    public DbSet<PageMetaTag> PageMetaTags => Set<PageMetaTag>();
    public DbSet<PageJsonLd> PageJsonLdBlocks => Set<PageJsonLd>();
    public DbSet<PageContentBlock> PageContentBlocks => Set<PageContentBlock>();
    public DbSet<InternalLink> InternalLinks => Set<InternalLink>();
    public DbSet<CrossRunLink> CrossRunLinks => Set<CrossRunLink>();
    public DbSet<PageRankScore> PageRankScores => Set<PageRankScore>();
    public DbSet<ComparisonCheck> ComparisonChecks => Set<ComparisonCheck>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<RunGate> RunGates => Set<RunGate>();
    public DbSet<CrawlPriorityUrlPattern> CrawlPriorityUrlPatterns => Set<CrawlPriorityUrlPattern>();
    public DbSet<TargetSiteBusinessProfile> TargetSiteBusinessProfiles => Set<TargetSiteBusinessProfile>();
    public DbSet<CompetitorPage> CompetitorPages => Set<CompetitorPage>();
    public DbSet<CompetitorPageHeading> CompetitorPageHeadings => Set<CompetitorPageHeading>();
    public DbSet<CompetitorPageMetaTag> CompetitorPageMetaTags => Set<CompetitorPageMetaTag>();
    public DbSet<CompetitorPageJsonLd> CompetitorPageJsonLdBlocks => Set<CompetitorPageJsonLd>();
    public DbSet<CompetitorCrawlProgressLog> CompetitorCrawlProgressLogs => Set<CompetitorCrawlProgressLog>();
    public DbSet<SerpRankSnapshot> SerpRankSnapshots => Set<SerpRankSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sa2");

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<SiteProfile>(e =>
        {
            e.ToTable("site_profiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.SiteUrl).HasMaxLength(2048).IsRequired();
            e.HasIndex(x => x.SiteUrl).IsUnique();
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.Property(x => x.BusinessType).HasMaxLength(128);
            e.HasOne(x => x.GeekSeoProject)
                .WithMany()
                .HasForeignKey(x => x.GeekSeoProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.GeekSeoProjectId);
            ConfigureJsonStringList(e, x => x.NicheTags);
            ConfigureJsonStringList(e, x => x.GeoAnchorNodes);
            ConfigureJsonStringList(e, x => x.CompetitorDomains);
            ConfigureJsonStringList(e, x => x.AuthorityPageUrls);
            ConfigureJsonStringList(e, x => x.WritingRecommendations);
        });

        modelBuilder.Entity<AnalysisRun>(e =>
        {
            e.ToTable("analysis_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Keyword).HasMaxLength(512).IsRequired();
            e.Property(x => x.TargetSiteUrl).HasMaxLength(2048).IsRequired();
            e.Property(x => x.SerpProviderKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.CurrentStage).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.SerpLanguageCode).HasMaxLength(16);
            e.Property(x => x.SerpDevice).HasMaxLength(32);
            e.Property(x => x.SerpOs).HasMaxLength(32);
            e.Property(x => x.SerpSeDomain).HasMaxLength(64);
            e.Property(x => x.SerpCheckUrl).HasMaxLength(2048);
            e.Property(x => x.CompetitorCrawlStatus).HasMaxLength(32);
            e.Property(x => x.CompetitorCrawlMessage).HasMaxLength(2048);
            e.Property(x => x.ResearchMode).HasMaxLength(16).HasDefaultValue(ResearchModes.Sa2);
            e.Property(x => x.TopicSlug).HasMaxLength(128);
            e.HasIndex(x => x.ProjectId);
            ConfigureJsonStringList(e, x => x.GapTopics);
        });

        modelBuilder.Entity<SerpItem>(e =>
        {
            e.ToTable("serp_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(32).IsRequired();
            e.Property(x => x.Position).HasMaxLength(16);
            e.Property(x => x.Domain).HasMaxLength(256);
            e.Property(x => x.Title).HasMaxLength(2048);
            e.Property(x => x.Url).HasMaxLength(2048);
            e.Property(x => x.CacheUrl).HasMaxLength(2048);
            e.Property(x => x.RelatedSearchUrl).HasMaxLength(2048);
            e.Property(x => x.Breadcrumb).HasMaxLength(2048);
            e.Property(x => x.WebsiteName).HasMaxLength(512);
            e.Property(x => x.FilterStatus).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.IncludeReason).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.ResearchLane).HasMaxLength(16);
            e.HasIndex(x => new { x.RunId, x.RankAbsolute });
            e.HasIndex(x => new { x.RunId, x.Type });
            e.HasIndex(x => new { x.RunId, x.ResearchLane });
            e.HasIndex(x => x.ProjectId);

            e.HasMany(x => x.Links)
                .WithOne(x => x.SerpItem)
                .HasForeignKey(x => x.SerpItemId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.HighlightedPhrases)
                .WithOne(x => x.SerpItem)
                .HasForeignKey(x => x.SerpItemId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.RelatedQueries)
                .WithOne(x => x.SerpItem)
                .HasForeignKey(x => x.SerpItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SerpItemLink>(e =>
        {
            e.ToTable("serp_item_links");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.HasIndex(x => x.SerpItemId);
        });

        modelBuilder.Entity<SerpItemHighlighted>(e =>
        {
            e.ToTable("serp_item_highlighted");
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasMaxLength(1024).IsRequired();
            e.HasIndex(x => x.SerpItemId);
        });

        modelBuilder.Entity<SerpRelatedQuery>(e =>
        {
            e.ToTable("serp_related_queries");
            e.HasKey(x => x.Id);
            e.Property(x => x.QueryText).HasMaxLength(2048).IsRequired();
            e.Property(x => x.QueryType).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => new { x.SerpItemId, x.Sequence });
        });

        modelBuilder.Entity<ReferenceExcludeDomain>(e =>
        {
            e.ToTable("reference_exclude_domains");
            e.HasKey(x => x.Id);
            e.Property(x => x.Domain).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<KnownPlatformDomain>(e =>
        {
            e.ToTable("known_platform_domains");
            e.HasKey(x => x.Id);
            e.Property(x => x.Domain).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<ProjectOwnedDomain>(e =>
        {
            e.ToTable("project_owned_domains");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<CompetitorSeedDomain>(e =>
        {
            e.ToTable("competitor_seed_domains");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<Page>(e =>
        {
            e.ToTable("pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.FetchMode).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.RunId);
            e.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<PageHeading>(e =>
        {
            e.ToTable("page_headings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PageId);
        });

        modelBuilder.Entity<PageMetaTag>(e =>
        {
            e.ToTable("page_meta_tags");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PageId);
        });

        modelBuilder.Entity<PageJsonLd>(e =>
        {
            e.ToTable("page_json_ld");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PageId);
        });

        modelBuilder.Entity<PageContentBlock>(e =>
        {
            e.ToTable("page_content_blocks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PageId);
        });

        modelBuilder.Entity<InternalLink>(e =>
        {
            e.ToTable("internal_links");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<CrossRunLink>(e =>
        {
            e.ToTable("cross_run_links");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<PageRankScore>(e =>
        {
            e.ToTable("page_rank_scores");
            e.HasKey(x => x.Id);
            e.Property(x => x.GraphScope).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<ComparisonCheck>(e =>
        {
            e.ToTable("comparison_checks");
            e.HasKey(x => x.Id);
            e.Property(x => x.FindingType).HasConversion<string>().HasMaxLength(64);
            e.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<Finding>(e =>
        {
            e.ToTable("findings");
            e.HasKey(x => x.Id);
            e.Property(x => x.FindingType).HasConversion<string>().HasMaxLength(64);
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<RunGate>(e =>
        {
            e.ToTable("run_gates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Stage).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.ValidationMessage).IsRequired();
            e.HasIndex(x => x.RunId);
        });

        modelBuilder.Entity<CrawlPriorityUrlPattern>(e =>
        {
            e.ToTable("crawl_priority_url_patterns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Pattern).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Pattern).IsUnique();
        });


        modelBuilder.Entity<CompetitorPage>(e =>
        {
            e.ToTable("competitor_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Domain).HasMaxLength(256).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.CanonicalUrl).HasMaxLength(2048);
            e.Property(x => x.FetchMode).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.RunId);
            e.HasIndex(x => new { x.RunId, x.Domain });

            e.HasMany(x => x.Headings)
                .WithOne(x => x.Page)
                .HasForeignKey(x => x.CompetitorPageId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.MetaTags)
                .WithOne(x => x.Page)
                .HasForeignKey(x => x.CompetitorPageId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.JsonLdBlocks)
                .WithOne(x => x.Page)
                .HasForeignKey(x => x.CompetitorPageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompetitorPageHeading>(e =>
        {
            e.ToTable("competitor_page_headings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired();
            e.HasIndex(x => x.CompetitorPageId);
        });

        modelBuilder.Entity<CompetitorPageMetaTag>(e =>
        {
            e.ToTable("competitor_page_meta_tags");
            e.HasKey(x => x.Id);
            e.Property(x => x.NameOrProperty).HasMaxLength(512).IsRequired();
            e.Property(x => x.Content).IsRequired();
            e.HasIndex(x => x.CompetitorPageId);
        });

        modelBuilder.Entity<CompetitorPageJsonLd>(e =>
        {
            e.ToTable("competitor_page_json_ld");
            e.HasKey(x => x.Id);
            e.Property(x => x.RawJson).IsRequired();
            e.Property(x => x.ParsedType).HasMaxLength(256);
            e.HasIndex(x => x.CompetitorPageId);
        });

        modelBuilder.Entity<CompetitorCrawlProgressLog>(e =>
        {
            e.ToTable("competitor_crawl_progress_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => new { x.RunId, x.Id });
        });

        modelBuilder.Entity<SerpRankSnapshot>(e =>
        {
            e.ToTable("serp_rank_snapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.TargetOrganicUrl).HasMaxLength(2048);
            e.HasIndex(x => new { x.RunId, x.ImportSequence }).IsUnique();
            e.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<TargetSiteBusinessProfile>(e =>
        {
            e.ToTable("target_site_business_profiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.TargetSiteUrl).HasMaxLength(2048).IsRequired();
            e.Property(x => x.BusinessType).HasMaxLength(256).IsRequired();
            e.Property(x => x.PrimaryServicesJson).IsRequired();
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.GeneratedSchemaJson).IsRequired();
            e.HasIndex(x => x.RunId).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.TargetSiteUrl });
        });
    }

    private static void ConfigureJsonStringList<TEntity>(
        EntityTypeBuilder<TEntity> entity,
        System.Linq.Expressions.Expression<Func<TEntity, List<string>>> propertyExpression)
        where TEntity : class =>
        entity.Property(propertyExpression)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .IsRequired(false);
}
