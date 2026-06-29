using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Api.Contracts;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.BusinessFocus;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Serp;
using System.Text.Json;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("projects")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateProjectResponse>> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            MaxCrawlDepth = request.MaxCrawlDepth ?? 4,
            MaxCrawlPages = request.MaxCrawlPages ?? 150
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return Ok(new CreateProjectResponse(project.Id, project.Name));
    }
}

[ApiController]
[Route("projects/{projectId:guid}/runs")]
public class ProjectRunsController(AppDbContext db, AnalysisRunOrchestrator orchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateRunResponse>> Create(
        Guid projectId,
        [FromBody] CreateRunRequest request,
        CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([projectId], ct);
        if (project is null) return NotFound();

        if (!SerpProviderPolicy.IsAllowedProviderKey(request.SerpProviderKey))
            return BadRequest(SerpProviderPolicy.RejectionMessage(request.SerpProviderKey));

        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Keyword = request.Keyword,
            TargetSiteUrl = request.TargetSiteUrl,
            SerpProviderKey = request.SerpProviderKey,
            IncludeReferenceDomains = request.IncludeReferenceDomains,
            Status = RunStatus.Running,
            CurrentStage = PipelineStage.Serp
        };

        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(ct);

        run = await orchestrator.StartRunAsync(run, ct);

        return Ok(new CreateRunResponse(run.Id, run.Status.ToString(), run.CurrentStage?.ToString()));
    }
}

[ApiController]
[Route("runs")]
public class RunsController(
    AppDbContext db,
    AnalysisRunOrchestrator orchestrator,
    SerpHtmlImportService serpHtmlImport,
    BusinessFocusClassificationService businessFocusClassificationService) : ControllerBase
{
    [HttpGet("{runId:guid}")]
    public async Task<ActionResult<RunSummaryResponse>> Get(Guid runId, CancellationToken ct)
    {
        var run = await db.AnalysisRuns
            .Include(r => r.RunGates)
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        if (run is null) return NotFound();

        var latestGate = run.RunGates.OrderByDescending(g => g.CheckedAt).FirstOrDefault();

        return Ok(new RunSummaryResponse(
            run.Id,
            run.ProjectId,
            run.Keyword,
            run.TargetSiteUrl,
            run.Status.ToString(),
            run.CurrentStage?.ToString(),
            latestGate?.ValidationMessage,
            run.RunGates.OrderBy(g => g.CheckedAt).Select(g => new GateSummary(
                g.Stage.ToString(), g.Passed, g.ValidationMessage, g.CheckedAt)).ToList()));
    }

    [HttpPost("{runId:guid}/reconcile")]
    public async Task<ActionResult<RunSummaryResponse>> Reconcile(Guid runId, CancellationToken ct)
    {
        var run = await orchestrator.ReconcileStuckRunAsync(runId, ct);
        if (run is null) return NotFound();
        return await Get(runId, ct);
    }

    [HttpPost("{runId:guid}/stages/{stage}/advance")]
    public async Task<ActionResult<RunSummaryResponse>> Advance(Guid runId, string stage, CancellationToken ct)
    {
        if (!Enum.TryParse<PipelineStage>(stage, ignoreCase: true, out var pipelineStage))
            return BadRequest($"Unknown stage: {stage}");

        try
        {
            await orchestrator.AdvanceStageAsync(runId, pipelineStage, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return await Get(runId, ct);
    }

    [HttpPost("{runId:guid}/serp/import-html")]
    public async Task<IActionResult> ImportSerpHtml(Guid runId, CancellationToken ct)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null) return NotFound();

        if (run.Status != RunStatus.Running || run.CurrentStage != PipelineStage.Serp)
            return BadRequest(new { error = "Run is not on the Serp stage." });

        if (await db.RunGates.AnyAsync(g => g.RunId == runId && g.Stage == PipelineStage.Serp, ct))
            return Conflict(new { error = "SERP stage already completed." });

        using var reader = new StreamReader(Request.Body);
        var html = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(html))
            return BadRequest(new { error = "Request body must contain saved Google SERP HTML." });

        try
        {
            var outcome = await serpHtmlImport.ImportHtmlAsync(run, html, run.Keyword, ct);
            return Ok(new
            {
                outcome.OrganicCount,
                outcome.PaaCount,
                outcome.GatePassed,
                outcome.GateMessage
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{runId:guid}/serp")]
    public async Task<IActionResult> GetSerp(Guid runId, [FromQuery] string? format, CancellationToken ct)
    {
        var run = await db.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null) return NotFound();

        var items = await db.SerpItems
            .AsNoTracking()
            .Include(i => i.Links)
            .Include(i => i.HighlightedPhrases)
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .OrderBy(i => i.RankAbsolute)
            .ToListAsync(ct);

        if (string.Equals(format, "live-advanced", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "report", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(SerpLiveAdvancedSerializer.Build(run, items));
        }

        if (string.Equals(format, "persisted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "db", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(SerpPersistedView.Build(run, items));
        }

        if (string.Equals(format, "filter", StringComparison.OrdinalIgnoreCase))
        {
            var organic = items
                .Where(i => i.Type == Domain.SerpItemTypes.Organic && !i.Ads)
                .ToList();
            return Ok(FilterInspectionView.Build(organic));
        }

        return Ok(SerpPersistedView.Build(run, items));
    }

    [HttpGet("{runId:guid}/business-profile")]
    public async Task<IActionResult> GetBusinessProfile(Guid runId, CancellationToken ct)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return NotFound();

        var extractGate = await db.RunGates
            .FirstOrDefaultAsync(g => g.RunId == runId && g.Stage == PipelineStage.Extract, ct);

        if (extractGate is not { Passed: true })
        {
            return NotFound(new BusinessProfileNotAvailableResponse(
                "business_profile_not_available",
                "extract_stage_not_completed"));
        }

        var profile = await db.TargetSiteBusinessProfiles.FirstOrDefaultAsync(p => p.RunId == runId, ct);
        if (profile is null)
        {
            return NotFound(new BusinessProfileNotAvailableResponse(
                "business_profile_not_available",
                "profile_not_generated"));
        }

        var primaryServices = JsonSerializer.Deserialize<List<string>>(profile.PrimaryServicesJson) ?? [];
        var generatedSchema = JsonSerializer.Deserialize<object>(profile.GeneratedSchemaJson) ?? new { };

        return Ok(new BusinessProfileResponse(
            profile.BusinessType,
            primaryServices,
            profile.ServiceArea,
            profile.Description,
            generatedSchema,
            profile.HasExistingSchema,
            profile.ExistingSchemaMatches,
            profile.GeneratedAt,
            profile.ReusedFromRunId));
    }

    [HttpPut("{runId:guid}/business-profile")]
    public async Task<IActionResult> UpsertBusinessProfile(
        Guid runId,
        [FromBody] UpsertBusinessProfileRequest request,
        CancellationToken ct)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return NotFound();

        try
        {
            await businessFocusClassificationService.UpsertManualProfileAsync(
                runId,
                new ManualBusinessProfileInput(
                    request.BusinessType,
                    request.PrimaryServices,
                    request.ServiceArea,
                    request.Description,
                    request.GeneratedSchemaJson.GetRawText(),
                    request.HasExistingSchema,
                    request.ExistingSchemaMatches),
                ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return await GetBusinessProfile(runId, ct);
    }

    [HttpGet("{runId:guid}/pages/{pageId:guid}/extraction")]
    public async Task<IActionResult> GetExtraction(Guid runId, Guid pageId, CancellationToken ct)
    {
        var page = await db.Pages.FirstOrDefaultAsync(p => p.RunId == runId && p.Id == pageId, ct);
        if (page is null) return NotFound();

        var headings = await db.PageHeadings.Where(h => h.PageId == pageId).OrderBy(h => h.Sequence).ToListAsync(ct);
        var meta = await db.PageMetaTags.Where(m => m.PageId == pageId).ToListAsync(ct);
        var jsonLd = await db.PageJsonLdBlocks.Where(j => j.PageId == pageId).ToListAsync(ct);
        var blocks = await db.PageContentBlocks.Where(b => b.PageId == pageId).OrderBy(b => b.Sequence).ToListAsync(ct);

        return Ok(new { page, headings, meta, jsonLd, blocks });
    }

    [HttpGet("{runId:guid}/graph")]
    public async Task<IActionResult> GetGraph(Guid runId, CancellationToken ct)
    {
        var internalLinks = await db.InternalLinks.Where(l => l.RunId == runId).ToListAsync(ct);
        var crossLinks = await db.CrossRunLinks.Where(l => l.RunId == runId).ToListAsync(ct);
        var scores = await db.PageRankScores.Where(s => s.RunId == runId).ToListAsync(ct);
        return Ok(new { internalLinks, crossLinks, scores });
    }

    [HttpGet("{runId:guid}/findings")]
    public async Task<IActionResult> GetFindings(Guid runId, CancellationToken ct)
    {
        var findings = await db.Findings.Where(f => f.RunId == runId).ToListAsync(ct);
        return Ok(findings);
    }

    [HttpGet("{runId:guid}/comparison-checks")]
    public async Task<IActionResult> GetComparisonChecks(Guid runId, CancellationToken ct)
    {
        var checks = await db.ComparisonChecks.Where(c => c.RunId == runId).ToListAsync(ct);
        return Ok(checks);
    }
}
