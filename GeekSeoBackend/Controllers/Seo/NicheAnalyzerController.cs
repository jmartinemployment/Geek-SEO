using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.NicheStepRunners;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/niche-analyzer")]
public sealed class NicheAnalyzerController(
    NicheAnalyzerService analyzer,
    INicheProfileRepository profileRepo,
    INicheAnalyticsDapperRepository analyticsRepo,
    ICurrentUserContext user,
    IServiceProvider services,
    WorkerUserContext workerUser,
    ILogger<NicheAnalyzerController> logger) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = user.RequireUserId();
            var profileId = await analyzer.EnqueueAsync(userId, request.ProjectId, request.Domain, request.SeedTopic, ct);
            return Ok(new { profileId, status = "pending", message = "Run each step manually in order." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{profileId:guid}/analysis-details")]
    public async Task<IActionResult> GetAnalysisDetails(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetAnalysisDetailsRowAsync(
                profileId,
                includeFusion: false,
                ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Analysis details unavailable for profile {ProfileId}: {Error}",
                    profileId,
                    result.Error);
                return StatusCode(503, new { error = "Details temporarily unavailable" });
            }

            if (result.Value is null)
                return Ok(new NicheAnalysisDetails(1, [], null, NicheStepCatalog.ToDtos()));

            var row = result.Value;
            if (!NicheAnalysisDetailsPolicy.IsStepLogAvailable(row.Status))
                return Ok(new NicheAnalysisDetails(row.AnalysisStepLogVersion, [], null, NicheStepCatalog.ToDtos()));

            var steps = NicheAnalysisStepLogJson.Parse(row.AnalysisStepLog);
            SiteTopicProfile? fusion = null;
            if (row.Status is "complete")
            {
                var fusionResult = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: true, ct);
                if (fusionResult.IsSuccess && fusionResult.Value is not null)
                {
                    fusion = await NicheStepRunState.LoadMergedFusionSnapshotAsync(
                        profileRepo,
                        profileId,
                        fusionResult.Value.FusionSnapshot,
                        steps,
                        ct);
                }
            }

            return Ok(new NicheAnalysisDetails(row.AnalysisStepLogVersion, steps, fusion, NicheStepCatalog.ToDtos()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching analysis details for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Details temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetStatusRowAsync(profileId, ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Status unavailable for profile {ProfileId}: {Error}",
                    profileId,
                    result.Error);
                return StatusCode(503, new { error = "Status temporarily unavailable" });
            }

            if (result.Value is null)
                return NotFound();

            var p = result.Value;
            var step = p.AnalysisStep ?? p.Status;
            var stepNumber = p.AnalysisStepNumber > 0
                ? p.AnalysisStepNumber
                : p.Status switch { "complete" => 14, _ => 0 };
            var totalSteps = p.AnalysisTotalSteps > 0 ? p.AnalysisTotalSteps : NicheStepCatalog.Ordered.Count;
            var stepStatusesResult = await profileRepo.GetStepStatusesAsync(profileId, ct);
            IReadOnlyDictionary<string, string>? stepStatuses = stepStatusesResult.IsSuccess
                && stepStatusesResult.Value is { Count: > 0 }
                ? stepStatusesResult.Value
                : null;

            IReadOnlyDictionary<string, string>? stepSummaries = null;
            IReadOnlyDictionary<string, string>? stepErrors = null;
            IReadOnlyDictionary<string, string>? stepWarnings = null;
            var stepRunsResult = await profileRepo.GetStepRunsAsync(profileId, ct);
            if (stepRunsResult.IsSuccess && stepRunsResult.Value is { Count: > 0 } runs)
            {
                stepSummaries = runs
                    .Where(r => !string.IsNullOrWhiteSpace(r.Summary))
                    .ToDictionary(r => r.StepSlug, r => r.Summary!, StringComparer.OrdinalIgnoreCase);
                stepErrors = runs
                    .Where(r =>
                        string.Equals(r.Status, "error", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(r.ErrorMessage))
                    .ToDictionary(r => r.StepSlug, r => r.ErrorMessage!, StringComparer.OrdinalIgnoreCase);
                stepWarnings = runs
                    .Select(r => (r.StepSlug, Warning: SerpValidationMessages.TryExtractWarning(r.Summary)))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Warning))
                    .ToDictionary(x => x.StepSlug, x => x.Warning!, StringComparer.OrdinalIgnoreCase);
            }

            return Ok(new NicheAnalysisStatus(
                p.Id,
                p.Status,
                step,
                stepNumber,
                totalSteps,
                p.ErrorMessage,
                p.CreatedAt,
                p.AnalysisProgressAt,
                p.StructureStatus,
                p.EnrichmentStatus,
                p.PersistStage,
                stepStatuses,
                stepSummaries,
                stepErrors,
                stepWarnings));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching niche status for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Status temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}")]
    public async Task<IActionResult> GetProfile(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetByIdAsync(profileId, ct);
            if (!result.IsSuccess || result.Value is null)
                return NotFound();

            return Ok(MapToResult(result.Value));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching niche profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Profile temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}/topic-candidates")]
    public async Task<IActionResult> GetTopicCandidates(
        Guid profileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? selectedOnly = null,
        CancellationToken ct = default)
    {
        try
        {
            user.RequireUserId();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var result = await profileRepo.GetTopicCandidatesAsync(profileId, page, pageSize, selectedOnly, ct);
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(503, new { error = result.Error ?? "Topic candidates temporarily unavailable" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching topic candidates for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Topic candidates temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}/coverage-matrix")]
    public async Task<IActionResult> GetCoverageMatrix(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await analyticsRepo.GetCoverageMatrixAsync(profileId, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            logger.LogWarning(
                "Coverage matrix Dapper path failed for {ProfileId}: {Error} — using relational fallback",
                profileId,
                result.Error);

            var profile = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profile.IsSuccess || profile.Value is null)
                return StatusCode(503, new { error = "Coverage matrix temporarily unavailable" });

            if (profile.Value.Pillars.Count == 0)
                return Ok(Array.Empty<PillarCoverageMatrix>());

            return Ok(NicheRelationalAnalyticsBuilder.BuildCoverageMatrix(profile.Value));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching coverage matrix for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Coverage matrix temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}/gaps")]
    public async Task<IActionResult> GetGaps(
        Guid profileId, [FromQuery] bool quickWinsOnly = false, CancellationToken ct = default)
    {
        try
        {
            user.RequireUserId();
            var result = await analyticsRepo.GetTopicalGapsAsync(profileId, quickWinsOnly, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            logger.LogWarning(
                "Topical gaps Dapper path failed for {ProfileId}: {Error} — using relational fallback",
                profileId,
                result.Error);

            var profile = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profile.IsSuccess || profile.Value is null)
                return StatusCode(503, new { error = "Gaps temporarily unavailable" });

            return Ok(NicheRelationalAnalyticsBuilder.BuildTopicalGaps(profile.Value, quickWinsOnly));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching topical gaps for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Gaps temporarily unavailable" });
        }
    }

    [HttpPost("{profileId:guid}/run-step/{slug}")]
    public IActionResult RunStep(Guid profileId, string slug, CancellationToken ct)
    {
        try
        {
            var userId = user.RequireUserId();
            _ = Task.Run(async () =>
            {
                workerUser.UserId = userId;
                try
                {
                    using var scope = services.CreateScope();
                    using var jobCt = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                    var stepRerunService = scope.ServiceProvider.GetRequiredService<NicheStepRerunService>();
                    var (success, error) = await stepRerunService.RerunStepAsync(
                        profileId, userId, slug, null, jobCt.Token);
                    if (!success)
                        logger.LogWarning("Step re-run {Slug} failed for {ProfileId}: {Error}", slug, profileId, error);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Step re-run {Slug} crashed for {ProfileId}", slug, profileId);
                    try
                    {
                        using var errorScope = services.CreateScope();
                        var failureRecorder = errorScope.ServiceProvider.GetRequiredService<NicheStepRerunService>();
                        NicheStepCatalog.BySlug.TryGetValue(slug, out var definition);
                        await failureRecorder.RecordStepFailureAsync(
                            profileId, userId, slug, definition, ex.Message, CancellationToken.None);
                    }
                    catch (Exception persistEx)
                    {
                        logger.LogError(
                            persistEx,
                            "Failed to persist crash state for step {Slug} profile {ProfileId}",
                            slug,
                            profileId);
                    }
                }
                finally
                {
                    workerUser.UserId = Guid.Empty;
                }
            }, CancellationToken.None);
            return Accepted(new { profileId, slug, message = $"Re-running step '{slug}'." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{profileId:guid}/step-statuses")]
    public async Task<IActionResult> GetStepStatuses(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetStepStatusesAsync(profileId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{profileId:guid}/niche-competitors")]
    public async Task<IActionResult> GetNicheCompetitors(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetCompetitorsAsync(profileId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value.Select(MapCompetitor).ToList());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching niche competitors for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Competitors temporarily unavailable" });
        }
    }

    [HttpPost("{profileId:guid}/analyze-competitors")]
    public async Task<IActionResult> AnalyzeCompetitors(Guid profileId, CancellationToken ct)
    {
        try
        {
            var userId = user.RequireUserId();
            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
                return NotFound(new { error = "Profile not found." });
            // Ownership validated implicitly — profile is scoped to user's project

            var competitors = profileResult.Value.Competitors.ToList();
            if (competitors.Count == 0)
                return BadRequest(new { error = "No competitors to analyze. Run niche analysis first." });

            _ = Task.Run(async () =>
            {
                workerUser.UserId = userId;
                try
                {
                    using var scope = services.CreateScope();
                    using var jobCt = new CancellationTokenSource(TimeSpan.FromHours(2));
                    var competitorService = scope.ServiceProvider.GetRequiredService<CompetitorAnalysisService>();
                    await competitorService.AnalyzeAsync(
                        profileId, userId, competitors, null, jobCt.Token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Competitor analysis failed for profile {ProfileId}", profileId);
                }
                finally
                {
                    workerUser.UserId = Guid.Empty;
                }
            }, CancellationToken.None);

            return Accepted(new { profileId, message = $"Competitor analysis started for {competitors.Count} site(s)." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{profileId:guid}/competitors")]
    public async Task<IActionResult> GetCompetitors(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await analyticsRepo.GetCompetitorOverlapAsync(profileId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching competitors for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Competitors temporarily unavailable" });
        }
    }

    [HttpGet("{profileId:guid}/entities")]
    public async Task<IActionResult> GetEntities(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await analyticsRepo.GetEntityCoverageAsync(profileId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching entities for profile {ProfileId}", profileId);
            return StatusCode(503, new { error = "Entities temporarily unavailable" });
        }
    }

    [HttpGet("project/{projectId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid projectId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetHistoryAsync(projectId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching niche history for project {ProjectId}", projectId);
            return StatusCode(503, new { error = "History temporarily unavailable" });
        }
    }

    [HttpGet("project/{projectId:guid}/progress")]
    public async Task<IActionResult> GetProgress(
        Guid projectId, [FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated)
            return Ok(Array.Empty<AuthorityProgressPoint>());

        try
        {
            var result = await analyticsRepo.GetAuthorityProgressAsync(projectId, months, ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Niche authority progress unavailable for project {ProjectId}: {Error}",
                    projectId,
                    result.Error);
            }

            return Ok(result.IsSuccess ? result.Value ?? [] : []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Niche authority progress failed for project {ProjectId}", projectId);
            return Ok(Array.Empty<AuthorityProgressPoint>());
        }
    }

    [HttpGet("project/{projectId:guid}/latest")]
    public async Task<IActionResult> GetLatest(Guid projectId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await profileRepo.GetLatestByProjectAsync(projectId, ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Failed to fetch latest niche profile for project {ProjectId}: {Error}",
                    projectId,
                    result.Error);
                return StatusCode(503, new { error = "Latest profile temporarily unavailable" });
            }

            if (result.Value is null) return NoContent();
            return Ok(MapToResult(result.Value));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (GeekDataGatewayExceptions.IsTransientGatewayFailure(ex, ct))
        {
            logger.LogWarning(ex, "Transient error fetching latest niche profile for project {ProjectId}", projectId);
            return StatusCode(503, new { error = "Latest profile temporarily unavailable" });
        }
    }

    private static T? TryDeserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return null;
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return null; }
    }

    private static NicheCompetitorResult MapCompetitor(NicheCompetitor c) => new(
        c.Id, c.Domain, c.SerpPresence, c.EstimatedAuthorityScore,
        c.PillarsRanking, c.StrengthAssessment, c.Scope,
        c.PagesCrawled, c.AvgWordCount, c.HasFaqSchema,
        TryDeserialize<List<string>>(c.ServicesJson),
        TryDeserialize<List<string>>(c.KnowsAboutJson),
        TryDeserialize<List<string>>(c.AreaServedJson),
        TryDeserialize<List<string>>(c.SameAsJson),
        c.Description, c.BrandName,
        TryDeserialize<List<CompetitorPillarResult>>(c.PillarsJson),
        c.CompetitorAnalyzedAt);

    private static NicheProfileResult MapToResult(NicheProfile p) => new()
    {
        Id = p.Id,
        ProjectId = p.ProjectId,
        Domain = p.Domain,
        PrimaryNiche = p.PrimaryNiche,
        NicheDescription = p.NicheDescription,
        NicheTags = p.NicheTags,
        AudienceType = p.AudienceType,
        CompetitionLevel = p.CompetitionLevel,
        TopicalAuthorityScore = p.TopicalAuthorityScore,
        TotalPillarsIdentified = p.TotalPillarsIdentified,
        PillarsCovered = p.PillarsCovered,
        PillarsPartial = p.PillarsPartial,
        PillarsGap = p.PillarsGap,
        AnalyzedAt = p.AnalyzedAt,
        NextAnalysisDue = p.NextAnalysisDue,
        CreatedAt = p.CreatedAt,
        Status = p.Status,
        StructureStatus = p.StructureStatus,
        EnrichmentStatus = p.EnrichmentStatus,
        Pillars = p.Pillars.OrderBy(x => x.DisplayOrder).Select(pi => new NichePillarResult
        {
            Id = pi.Id,
            PillarTopic = pi.PillarTopic,
            PillarSlug = pi.PillarSlug,
            PrimaryKeyword = pi.PrimaryKeyword,
            PageUrl = pi.PageUrl,
            SearchIntent = pi.SearchIntent,
            SearchVolume = pi.SearchVolume,
            KeywordDifficulty = pi.KeywordDifficulty,
            CoverageStatus = pi.CoverageStatus,
            CoverageScore = pi.CoverageScore,
            ExistingPageCount = pi.ExistingPageCount,
            RequiredSubtopicCount = pi.RequiredSubtopicCount,
            CoveredSubtopicCount = pi.CoveredSubtopicCount,
            StrategicPriority = pi.StrategicPriority,
            ContentAngle = pi.ContentAngle,
            Source = pi.Source,
            DisplayOrder = pi.DisplayOrder,
            Subtopics = pi.Subtopics.Select(s => new NicheSubtopicResult
            {
                Id = s.Id,
                SubtopicTitle = s.SubtopicTitle,
                TargetKeyword = s.TargetKeyword,
                SearchIntent = s.SearchIntent,
                SearchVolume = s.SearchVolume,
                KeywordDifficulty = s.KeywordDifficulty,
                CoverageStatus = s.CoverageStatus,
                ExistingUrl = s.ExistingUrl,
                RecommendedFormat = s.RecommendedFormat,
                RecommendedWordCount = s.RecommendedWordCount,
                FixEffort = s.FixEffort,
                IsQuickWin = s.IsQuickWin,
            }).ToList(),
            PaaQuestions = TryDeserialize<List<PaaQuestionItem>>(pi.PaaQuestionsJson) ?? [],
            RelatedSearches = TryDeserialize<List<string>>(pi.RelatedSearchesJson) ?? [],
            LocalPaaQuestions = TryDeserialize<List<PaaQuestionItem>>(pi.LocalPaaQuestionsJson) ?? [],
            LocalRelatedSearches = TryDeserialize<List<string>>(pi.LocalRelatedSearchesJson) ?? [],
        }).ToList(),
        Competitors = p.Competitors.Select(MapCompetitor).ToList(),
        Entities = p.Entities.Select(e => new NicheEntityResult(
            e.Id, e.EntityName, e.EntityType,
            e.MentionFrequency, e.PresentOnDomain, e.AssociatedPillarIds)).ToList(),
    };
}

public sealed record AnalyzeRequest(Guid ProjectId, string Domain, string? SeedTopic = null);
