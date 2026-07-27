using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IPerformanceService
{
    Task<ContentPerformance> RecordPerformance(Guid publicationId, Guid assetVersionId, string url);
    Task UpdatePerformanceMetrics(Guid perfId, int views, int engaged, int conversions, decimal timeOnPage, decimal bounce);
    Task RecordInsightContribution(Guid perfId, Guid insightId, int contributionScore, bool isKeyDiff, string? notes);
    Task<InsightFeedback> AggregateInsightPerformance(Guid insightId);
    Task<List<Guid>> GetRetirementCandidates();
    Task<InsightPerformanceRecommendation> GetInsightRecommendation(Guid insightId);
}

public class PerformanceService : IPerformanceService
{
    public Task<ContentPerformance> RecordPerformance(Guid publicationId, Guid assetVersionId, string url)
    {
        var perf = new ContentPerformance(publicationId, assetVersionId, url);
        // Caller persists
        return Task.FromResult(perf);
    }

    public Task UpdatePerformanceMetrics(Guid perfId, int views, int engaged, int conversions, decimal timeOnPage, decimal bounce)
    {
        // Validation
        if (engaged > views) throw new ArgumentException("Engaged views cannot exceed total views");
        if (bounce < 0 || bounce > 100) throw new ArgumentException("Bounce rate must be 0-100");

        // Caller updates and persists
        return Task.CompletedTask;
    }

    public Task RecordInsightContribution(Guid perfId, Guid insightId, int contributionScore, bool isKeyDiff, string? notes)
    {
        if (contributionScore < 1 || contributionScore > 10)
            throw new ArgumentException("Contribution score must be 1-10");

        // Link will be created by caller
        return Task.CompletedTask;
    }

    public Task<InsightFeedback> AggregateInsightPerformance(Guid insightId)
    {
        // Would be called with DB access to compute averages
        var feedback = new InsightFeedback(insightId);
        // Caller computes and updates metrics
        return Task.FromResult(feedback);
    }

    public async Task<List<Guid>> GetRetirementCandidates()
    {
        // Return insight IDs where ShouldBeRetired = true
        // Caller queries DB
        return await Task.FromResult(new List<Guid>());
    }

    public async Task<InsightPerformanceRecommendation> GetInsightRecommendation(Guid insightId)
    {
        // Caller queries DB for insight feedback
        return await Task.FromResult(new InsightPerformanceRecommendation
        {
            InsightId = insightId,
            Status = InsightStatus.Unrated,
            Recommendation = "Insufficient data"
        });
    }
}

public class InsightPerformanceRecommendation
{
    public Guid InsightId { get; set; }
    public InsightStatus Status { get; set; }
    public decimal AverageScore { get; set; }
    public int UsageCount { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

public enum InsightStatus
{
    Unrated,           // Used in no published content yet
    ProvenWinner,      // Avg score >= 7, used 2+
    Solid,             // Avg score 5-6.9
    Struggling,        // Avg score 3-4.9
    RetirementCandidate // Avg score < 3, used 3+
}
