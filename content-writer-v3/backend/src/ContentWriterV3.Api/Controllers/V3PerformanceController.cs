using ContentWriterV3.Application.Services;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/performance")]
public class V3PerformanceController : ControllerBase
{
    private readonly ContentWriterV3DbContext _db;
    private readonly IPerformanceService _performanceService;

    public V3PerformanceController(ContentWriterV3DbContext db, IPerformanceService performanceService)
    {
        _db = db;
        _performanceService = performanceService;
    }

    [HttpPost("record")]
    public async Task<IActionResult> RecordPerformance([FromBody] RecordPerformanceRequest request)
    {
        var perf = await _performanceService.RecordPerformance(
            request.PublicationId,
            request.AssetVersionId,
            request.PublishedUrl
        );

        _db.ContentPerformances.Add(perf);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPerformance), new { perfId = perf.Id }, new { id = perf.Id });
    }

    [HttpGet("{perfId}")]
    public async Task<IActionResult> GetPerformance(Guid perfId)
    {
        var perf = await _db.ContentPerformances
            .Include(p => p.InsightPerformanceLinks)
            .FirstOrDefaultAsync(p => p.Id == perfId);

        if (perf == null) return NotFound();

        return Ok(new
        {
            id = perf.Id,
            publicationId = perf.PublicationId,
            assetVersionId = perf.AssetVersionId,
            publishedUrl = perf.PublishedUrl,
            publishedDate = perf.PublishedDate,
            views = perf.Views,
            engagedViews = perf.EngagedViews,
            conversions = perf.Conversions,
            avgTimeOnPage = perf.AvgTimeOnPage,
            bounceRate = perf.BounceRate,
            rankPosition = perf.RankPosition,
            qualityScore = perf.EstimateQualityScore(),
            insightContributions = perf.InsightPerformanceLinks.Select(l => new
            {
                insightId = l.ResearchInsightId,
                contributionScore = l.ContributionScore,
                isKeyDifferentiator = l.IsKeyDifferentiator,
                feedbackNotes = l.FeedbackNotes
            }),
            lastSyncedAt = perf.LastSyncedAt
        });
    }

    [HttpPatch("{perfId}/metrics")]
    public async Task<IActionResult> UpdateMetrics(Guid perfId, [FromBody] UpdateMetricsRequest request)
    {
        var perf = await _db.ContentPerformances.FirstOrDefaultAsync(p => p.Id == perfId);
        if (perf == null) return NotFound();

        await _performanceService.UpdatePerformanceMetrics(
            perfId,
            request.Views,
            request.EngagedViews,
            request.Conversions,
            request.AvgTimeOnPage,
            request.BounceRate
        );

        perf.UpdateMetrics(request.Views, request.EngagedViews, request.Conversions, request.AvgTimeOnPage, request.BounceRate);
        _db.ContentPerformances.Update(perf);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{perfId}/insight-contribution")]
    public async Task<IActionResult> RecordInsightContribution(Guid perfId, [FromBody] InsightContributionRequest request)
    {
        var perf = await _db.ContentPerformances.FirstOrDefaultAsync(p => p.Id == perfId);
        if (perf == null) return NotFound("Performance record not found");

        await _performanceService.RecordInsightContribution(
            perfId,
            request.InsightId,
            request.ContributionScore,
            request.IsKeyDifferentiator,
            request.FeedbackNotes
        );

        var link = new InsightPerformanceLink(perfId, request.InsightId)
        {
            ContributionScore = request.ContributionScore,
            IsKeyDifferentiator = request.IsKeyDifferentiator,
            FeedbackNotes = request.FeedbackNotes
        };

        _db.InsightPerformanceLinks.Add(link);
        await _db.SaveChangesAsync();

        return Created("", new { linkId = link.Id });
    }

    [HttpGet("insights/{insightId}/recommendation")]
    public async Task<IActionResult> GetInsightRecommendation(Guid insightId)
    {
        var recommendation = await _performanceService.GetInsightRecommendation(insightId);

        // Query DB for actual feedback
        var feedback = await _db.InsightFeedbacks.FirstOrDefaultAsync(f => f.ResearchInsightId == insightId);

        if (feedback != null)
        {
            var successRate = feedback.TimesUsed > 0 ? (feedback.TimesSuccessful / (decimal)feedback.TimesUsed) * 100 : 0;
            var status = feedback.ShouldBeRetired
                ? InsightStatus.RetirementCandidate
                : feedback.AveragePerformanceScore >= 7 ? InsightStatus.ProvenWinner
                : feedback.AveragePerformanceScore >= 5 ? InsightStatus.Solid
                : feedback.AveragePerformanceScore >= 3 ? InsightStatus.Struggling
                : InsightStatus.Unrated;

            return Ok(new
            {
                insightId,
                status = status.ToString(),
                averageScore = feedback.AveragePerformanceScore,
                usageCount = feedback.TimesUsed,
                successRate = Math.Round(successRate, 1),
                recommendation = GetRecommendationText(status),
                strengths = feedback.WhatWorkedWellJson,
                weaknesses = feedback.WhyItsStruggling,
                shouldRetire = feedback.ShouldBeRetired
            });
        }

        return Ok(new
        {
            insightId,
            status = InsightStatus.Unrated.ToString(),
            averageScore = 0m,
            usageCount = 0,
            successRate = 0m,
            recommendation = "Not yet used in published content"
        });
    }

    [HttpGet("insights/recommendations/list")]
    public async Task<IActionResult> GetRecommendedInsights([FromQuery] string filter = "all")
    {
        var feedbacks = await _db.InsightFeedbacks
            .Where(f => f.TimesUsed > 0)
            .OrderByDescending(f => f.AveragePerformanceScore)
            .ToListAsync();

        var filtered = filter switch
        {
            "winners" => feedbacks.Where(f => f.AveragePerformanceScore >= 7).ToList(),
            "struggling" => feedbacks.Where(f => f.AveragePerformanceScore < 5).ToList(),
            "retire" => feedbacks.Where(f => f.ShouldBeRetired).ToList(),
            _ => feedbacks
        };

        return Ok(new
        {
            filter,
            count = filtered.Count,
            insights = filtered.Select(f => new
            {
                insightId = f.ResearchInsightId,
                averageScore = f.AveragePerformanceScore,
                usageCount = f.TimesUsed,
                successCount = f.TimesSuccessful,
                successRate = f.TimesUsed > 0 ? Math.Round((f.TimesSuccessful / (decimal)f.TimesUsed) * 100, 1) : 0,
                shouldRetire = f.ShouldBeRetired
            })
        });
    }

    private string GetRecommendationText(InsightStatus status) => status switch
    {
        InsightStatus.ProvenWinner => "Consistently high performance. Use in new content.",
        InsightStatus.Solid => "Solid performer. Works in most contexts.",
        InsightStatus.Struggling => "Underperforming. Revise or skip in future content.",
        InsightStatus.RetirementCandidate => "Retire this insight. Has underperformed in 3+ uses.",
        _ => "Insufficient data to recommend."
    };
}

public class RecordPerformanceRequest
{
    public Guid PublicationId { get; set; }
    public Guid AssetVersionId { get; set; }
    public string PublishedUrl { get; set; } = string.Empty;
}

public class UpdateMetricsRequest
{
    public int Views { get; set; }
    public int EngagedViews { get; set; }
    public int Conversions { get; set; }
    public decimal AvgTimeOnPage { get; set; }
    public decimal BounceRate { get; set; }
}

public class InsightContributionRequest
{
    public Guid InsightId { get; set; }
    public int ContributionScore { get; set; }
    public bool IsKeyDifferentiator { get; set; }
    public string? FeedbackNotes { get; set; }
}
