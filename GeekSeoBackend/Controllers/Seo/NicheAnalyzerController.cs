using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/niche-analyzer")]
public sealed class NicheAnalyzerController(
    NicheAnalyzerService analyzer,
    INicheProfileRepository profileRepo,
    INicheAnalyticsDapperRepository analyticsRepo,
    ICurrentUserContext user,
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

            // NicheAnalysisJobWorker dequeues profiles with status "queued" (every ~5s).
            return Ok(new { profileId, status = "queued" });
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
            var result = await profileRepo.GetByIdAsync(profileId, ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Analysis details unavailable for profile {ProfileId}: {Error}",
                    profileId,
                    result.Error);
                return StatusCode(503, new { error = "Details temporarily unavailable" });
            }

            if (result.Value is null)
                return Ok(new NicheAnalysisDetails(1, [], null));

            var profile = result.Value;
            if (!NicheAnalysisDetailsPolicy.IsStepLogAvailable(profile.Status))
                return Ok(new NicheAnalysisDetails(profile.AnalysisStepLogVersion, [], null));

            var steps = NicheAnalysisStepLogJson.Parse(profile.AnalysisStepLog);
            var fusion = SiteTopicProfileJson.Parse(profile.FusionSnapshot);
            return Ok(new NicheAnalysisDetails(profile.AnalysisStepLogVersion, steps, fusion));
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
            var result = await profileRepo.GetByIdAsync(profileId, ct);
            if (!result.IsSuccess || result.Value is null)
                return NotFound();

            var p = result.Value;
            var step = p.AnalysisStep ?? p.Status;
            var stepNumber = p.AnalysisStepNumber > 0
                ? p.AnalysisStepNumber
                : p.Status switch { "complete" => 14, _ => 0 };
            var totalSteps = p.AnalysisTotalSteps > 0 ? p.AnalysisTotalSteps : 14;
            return Ok(new NicheAnalysisStatus(
                p.Id, p.Status, step, stepNumber, totalSteps, p.ErrorMessage, p.CreatedAt, p.AnalysisProgressAt));
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

    [HttpGet("{profileId:guid}/coverage-matrix")]
    public async Task<IActionResult> GetCoverageMatrix(Guid profileId, CancellationToken ct)
    {
        try
        {
            user.RequireUserId();
            var result = await analyticsRepo.GetCoverageMatrixAsync(profileId, ct);
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
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
            if (!result.IsSuccess) return StatusCode(500, new { error = result.Error });
            return Ok(result.Value);
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
        }).ToList(),
        Competitors = p.Competitors.Select(c => new NicheCompetitorResult(
            c.Id, c.Domain, c.SerpPresence, c.EstimatedAuthorityScore,
            c.PillarsRanking, c.StrengthAssessment)).ToList(),
        Entities = p.Entities.Select(e => new NicheEntityResult(
            e.Id, e.EntityName, e.EntityType,
            e.MentionFrequency, e.PresentOnDomain, e.AssociatedPillarIds)).ToList(),
    };
}

public sealed record AnalyzeRequest(Guid ProjectId, string Domain, string? SeedTopic = null);
