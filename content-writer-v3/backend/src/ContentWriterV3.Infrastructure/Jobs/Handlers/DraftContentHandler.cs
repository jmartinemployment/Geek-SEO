using ContentWriterV3.Application.Services;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContentWriterV3.Infrastructure.Jobs.Handlers;

public class DraftContentHandler : JobHandler<DraftContentPayload>
{
    private readonly ContentWriterV3DbContext _db;
    private readonly IContentGenerator _contentGenerator;
    private readonly IContentIntelligenceValidator _validator;

    public override string JobType => "DraftContent";

    public DraftContentHandler(ContentWriterV3DbContext db, IContentGenerator contentGenerator, IContentIntelligenceValidator validator)
    {
        _db = db;
        _contentGenerator = contentGenerator;
        _validator = validator;
    }

    protected override async Task ExecuteAsync(Job job, DraftContentPayload payload, CancellationToken cancellationToken)
    {
        // Load strategy brief with related data
        var brief = await _db.StrategyBriefs
            .Include(sb => sb.ApprovalHistory)
            .FirstOrDefaultAsync(sb => sb.Id == payload.StrategyBriefId);
        if (brief == null) throw new InvalidOperationException($"StrategyBrief {payload.StrategyBriefId} not found");

        // Load insights and site audit context
        var siteAudit = await _db.SiteAudits
            .Include(sa => sa.ContentInventory)
            .Include(sa => sa.TopicalClusters)
            .FirstOrDefaultAsync(sa => sa.ClientId == brief.CampaignId); // Simplified: should link properly

        var insights = await _db.ResearchInsights
            .Where(ri => ri.ResearchRunId == payload.ResearchRunId && ri.IncludeInOutline)
            .OrderBy(ri => ri.OrderIndex)
            .ToListAsync();

        // Generate draft with site context
        var draftContent = await _contentGenerator.GenerateDraft(new DraftGenerationContext
        {
            StrategyBrief = brief,
            Insights = insights,
            SiteAudit = siteAudit,
            ResearchRunId = payload.ResearchRunId
        });

        // Validate draft against site intelligence
        var draft = new ContentAssetVersion
        {
            AssetId = payload.ContentAssetId,
            BodyDocumentJson = draftContent,
            Status = "Draft"
        };

        // Run validation if site audit exists
        if (siteAudit != null)
        {
            var validationResult = _validator.ValidateAgainstSiteContext(draft, siteAudit, brief);
            draft.ValidationWarningsJson = JsonSerializer.Serialize(validationResult.Warnings);
            draft.ValidationRecommendationsJson = JsonSerializer.Serialize(validationResult.Recommendations);
        }

        _db.ContentAssetVersions.Add(draft);
        job.MarkCompleted();

        await _db.SaveChangesAsync();
    }
}

public class DraftContentPayload
{
    public Guid StrategyBriefId { get; set; }
    public Guid ContentAssetId { get; set; }
    public Guid ResearchRunId { get; set; }
}
