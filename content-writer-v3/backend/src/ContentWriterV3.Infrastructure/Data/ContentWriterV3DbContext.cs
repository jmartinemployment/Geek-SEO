using ContentWriterV3.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Infrastructure.Data;

public class ContentWriterV3DbContext : DbContext
{
    public const string SchemaName = "content_writer_v3";

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<ClientProfileVersion> ClientProfileVersions => Set<ClientProfileVersion>();
    public DbSet<ClientBrandVoiceLink> ClientBrandVoiceLinks => Set<ClientBrandVoiceLink>();
    public DbSet<ContentCampaign> ContentCampaigns => Set<ContentCampaign>();
    public DbSet<ContentAsset> ContentAssets => Set<ContentAsset>();
    public DbSet<ContentAssetVersion> ContentAssetVersions => Set<ContentAssetVersion>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<PainPoint> PainPoints => Set<PainPoint>();
    public DbSet<PainPointEvidenceLink> PainPointEvidenceLinks => Set<PainPointEvidenceLink>();

    // Phase 1: Research & Intelligence
    public DbSet<ResearchRun> ResearchRuns => Set<ResearchRun>();
    public DbSet<ResearchSource> ResearchSources => Set<ResearchSource>();
    public DbSet<ResearchEvidence> ResearchEvidence => Set<ResearchEvidence>();
    public DbSet<ReconciliationProposal> ReconciliationProposals => Set<ReconciliationProposal>();

    public ContentWriterV3DbContext(DbContextOptions<ContentWriterV3DbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        // Configure composite keys and relationships
        ConfigureWorkspace(modelBuilder);
        ConfigureClient(modelBuilder);
        ConfigureClientProfile(modelBuilder);
        ConfigureContentCampaign(modelBuilder);
        ConfigureContentAsset(modelBuilder);
        ConfigureJob(modelBuilder);
        ConfigurePainPoint(modelBuilder);
    }

    private static void ConfigureWorkspace(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>()
            .HasKey(w => w.Id);

        modelBuilder.Entity<Workspace>()
            .Property(w => w.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<Workspace>()
            .HasMany(w => w.Clients)
            .WithOne()
            .HasForeignKey(c => c.WorkspaceId);
    }

    private static void ConfigureClient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Client>()
            .Property(c => c.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<Client>()
            .HasIndex(c => new { c.WorkspaceId, c.Name })
            .IsUnique();
    }

    private static void ConfigureClientProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientProfile>()
            .HasKey(cp => cp.Id);

        modelBuilder.Entity<ClientProfileVersion>()
            .HasKey(cpv => cpv.Id);

        modelBuilder.Entity<ClientProfileVersion>()
            .Property(cpv => cpv.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<ClientBrandVoiceLink>()
            .HasKey(cbvl => cbvl.Id);
    }

    private static void ConfigureContentCampaign(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentCampaign>()
            .HasKey(cc => cc.Id);

        modelBuilder.Entity<ContentCampaign>()
            .Property(cc => cc.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<ContentCampaign>()
            .HasIndex(cc => new { cc.ClientId, cc.Name })
            .IsUnique();

        modelBuilder.Entity<ContentCampaign>()
            .HasMany(cc => cc.Assets)
            .WithOne()
            .HasForeignKey(ca => ca.CampaignId);
    }

    private static void ConfigureContentAsset(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentAsset>()
            .HasKey(ca => ca.Id);

        modelBuilder.Entity<ContentAssetVersion>()
            .HasKey(cav => cav.Id);

        modelBuilder.Entity<ContentAssetVersion>()
            .Property(cav => cav.Version)
            .IsConcurrencyToken();
    }

    private static void ConfigureJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>()
            .HasKey(j => j.Id);

        modelBuilder.Entity<Job>()
            .HasIndex(j => j.IdempotencyKey)
            .IsUnique();

        modelBuilder.Entity<Job>()
            .Property(j => j.Version)
            .IsConcurrencyToken();
    }

    private static void ConfigurePainPoint(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PainPoint>()
            .HasKey(pp => pp.Id);

        modelBuilder.Entity<PainPoint>()
            .HasIndex(pp => new { pp.ClientId, pp.Name })
            .IsUnique();

        modelBuilder.Entity<PainPointEvidenceLink>()
            .HasKey(ppel => ppel.Id);
    }
}
