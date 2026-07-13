using ContentWriter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ContentWriter.Infrastructure.Data;

public class ContentWriterDbContext : DbContext
{
    // ASCII Unit Separator: joins/splits List<string> properties into a single nvarchar(max) column.
    // Not expected to occur naturally in scraped HTML/text content.
    private static readonly char[] ListSeparator = { (char)0x1F };

    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());

    public ContentWriterDbContext(DbContextOptions<ContentWriterDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<CrawledSite> CrawledSites => Set<CrawledSite>();
    public DbSet<KeywordSource> KeywordSources => Set<KeywordSource>();
    public DbSet<GeneratedContent> GeneratedContents => Set<GeneratedContent>();
    public DbSet<ContentFigure> ContentFigures => Set<ContentFigure>();
    public DbSet<ProjectPublication> ProjectPublications => Set<ProjectPublication>();

    private static string JoinList(List<string> values) => string.Join(ListSeparator[0], values);

    private static List<string> SplitList(string value) =>
        value.Length == 0 ? new List<string>() : value.Split(ListSeparator, StringSplitOptions.None).ToList();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ContentWriterDbContextOptionsExtensions.SchemaName);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(256).IsRequired();
            entity.Property(p => p.ProjectUrl).HasMaxLength(2048).IsRequired();
            entity.Property(p => p.TargetKeyword).HasMaxLength(256);
            entity.Property(p => p.ToolsGenerationOutcome).HasMaxLength(64);

            entity.HasOne(p => p.CrawledSite)
                  .WithOne(c => c.Project)
                  .HasForeignKey<CrawledSite>(c => c.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.KeywordSources)
                  .WithOne(k => k.Project)
                  .HasForeignKey(k => k.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.GeneratedContents)
                  .WithOne(g => g.Project)
                  .HasForeignKey(g => g.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Publications)
                  .WithOne(pub => pub.Project)
                  .HasForeignKey(pub => pub.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<ContentFigure>()
                  .WithOne(f => f.Project)
                  .HasForeignKey(f => f.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CrawledSite>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.JsonLdBlocks)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
            entity.Property(c => c.Headings)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
            entity.Property(c => c.Paragraphs)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
        });

        modelBuilder.Entity<KeywordSource>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.RawContent);
            entity.Property(k => k.ExtractedHeadings)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
            entity.Property(k => k.ExtractedParagraphs)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
            entity.Property(k => k.ExtractedQuestions)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
        });

        modelBuilder.Entity<GeneratedContent>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).HasMaxLength(512);
            entity.Property(g => g.DisplayTitle).HasMaxLength(512);
            entity.Property(g => g.Slug).HasMaxLength(512);
            entity.Property(g => g.BodyHtml);
            entity.Property(g => g.HomeUseCaseExcerpt).HasMaxLength(2000);
            entity.Property(g => g.DepartmentListExcerpt).HasMaxLength(2000);
            entity.Property(g => g.HeroExcerpt).HasMaxLength(2000);
            entity.Property(g => g.NewspaperExcerpt).HasMaxLength(2000);
            entity.Property(g => g.PillarPageUseCaseExcerpt).HasMaxLength(2000);
            entity.Property(g => g.ToolPageExcerpt).HasMaxLength(2000);
            entity.Property(g => g.Advertisement).HasMaxLength(4000);
            entity.Property(g => g.JsonLdSchema);
            entity.Property(g => g.MetaDescription);
            entity.Property(g => g.HeroImageUrl).HasMaxLength(2048);
            entity.Property(g => g.SourceAppName).HasMaxLength(512);
            entity.Property(g => g.Keywords)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
            entity.Property(g => g.SectionOutline)
                  .HasConversion(v => JoinList(v), v => SplitList(v), StringListComparer);
        });

        modelBuilder.Entity<ProjectPublication>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.GeekApiSlug).HasMaxLength(512).IsRequired();
            entity.HasIndex(p => new { p.ProjectId, p.ContentType, p.GeekApiSlug }).IsUnique();
        });

        modelBuilder.Entity<ContentFigure>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.SourceType).HasMaxLength(576).IsRequired();
            entity.Property(f => f.HeadingSlug).HasMaxLength(512).IsRequired();
            entity.Property(f => f.Heading).HasMaxLength(512).IsRequired();
            entity.Property(f => f.BriefText).IsRequired();
            entity.Property(f => f.SkipReason).HasMaxLength(64);
            entity.Property(f => f.ImageUrl).HasMaxLength(2048);
            entity.Property(f => f.ImageRelativePath).HasMaxLength(1024);
            entity.Property(f => f.ImageStorage).HasMaxLength(32).IsRequired();
            entity.Property(f => f.ImageAlt).HasMaxLength(512).IsRequired();
            entity.Property(f => f.GeekApiSlug).HasMaxLength(512);

            entity.HasIndex(f => new { f.ProjectId, f.SourceType, f.HeadingSlug }).IsUnique();
            entity.HasIndex(f => new { f.ProjectId, f.SourceType, f.SectionOrder });

            entity.HasOne(f => f.ImagePromptContent)
                  .WithMany()
                  .HasForeignKey(f => f.ImagePromptContentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
