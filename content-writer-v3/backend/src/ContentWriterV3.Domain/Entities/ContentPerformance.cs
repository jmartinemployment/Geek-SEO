namespace ContentWriterV3.Domain.Entities;

public class ContentPerformance : BaseEntity
{
    public Guid PublicationId { get; set; }
    public Guid AssetVersionId { get; set; }
    public string PublishedUrl { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public DateTime? MeasuredUntil { get; set; } // Last time we synced metrics
    public int Views { get; set; }
    public int EngagedViews { get; set; } // Scrolled past 50%
    public int Conversions { get; set; } // CTA clicked
    public decimal AvgTimeOnPage { get; set; } // seconds
    public decimal BounceRate { get; set; } // 0-100
    public int? RankPosition { get; set; } // If tracked via GSC
    public string? TrackingSource { get; set; } // Google Analytics, custom tracking, etc.
    public List<InsightPerformanceLink> InsightPerformanceLinks { get; set; } = new();
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    public ContentPerformance() { }

    public ContentPerformance(Guid publicationId, Guid assetVersionId, string url)
    {
        PublicationId = publicationId;
        AssetVersionId = assetVersionId;
        PublishedUrl = url;
    }

    public void UpdateMetrics(int views, int engaged, int conversions, decimal timeOnPage, decimal bounce)
    {
        Views = views;
        EngagedViews = engaged;
        Conversions = conversions;
        AvgTimeOnPage = timeOnPage;
        BounceRate = bounce;
        LastSyncedAt = DateTime.UtcNow;
    }

    public int EstimateQualityScore()
    {
        // Simple heuristic: high engagement + conversions + low bounce = good content
        var engagementRatio = Views > 0 ? (decimal)EngagedViews / Views : 0;
        var conversionRatio = Views > 0 ? (decimal)Conversions / Views : 0;
        var bounceNormalized = (100 - BounceRate) / 100m;

        var score = (int)((engagementRatio * 0.4m + conversionRatio * 0.4m + bounceNormalized * 0.2m) * 10);
        return Math.Min(10, Math.Max(1, score));
    }
}

public class InsightPerformanceLink : BaseEntity
{
    public Guid ContentPerformanceId { get; set; }
    public Guid ResearchInsightId { get; set; }
    public int ContributionScore { get; set; } // 1-10: how much did this insight drive performance?
    public string? FeedbackNotes { get; set; } // "This insight was the hook that got people to scroll"
    public bool IsKeyDifferentiator { get; set; } // Did this insight stand out vs competitors?

    public InsightPerformanceLink() { }

    public InsightPerformanceLink(Guid perfId, Guid insightId)
    {
        ContentPerformanceId = perfId;
        ResearchInsightId = insightId;
    }
}

public class InsightFeedback : BaseEntity
{
    public Guid ResearchInsightId { get; set; }
    public decimal AveragePerformanceScore { get; set; } // 1-10 across all uses
    public int TimesUsed { get; set; } // How many pieces used this insight
    public int TimesSuccessful { get; set; } // How many had good performance
    public List<string> WhatWorkedWellJson { get; set; } = new(); // "The 'cost of inaction' framing drives engagement"
    public List<string> WhyItsStruggling { get; set; } = new(); // "Too obvious to readers", "Not different vs competitors"
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public bool ShouldBeRetired { get; set; } // Consistently underperforms

    public InsightFeedback() { }

    public InsightFeedback(Guid insightId)
    {
        ResearchInsightId = insightId;
    }

    public void UpdateFeedback(decimal avgScore, int timesUsed, int timesSuccessful)
    {
        AveragePerformanceScore = avgScore;
        TimesUsed = timesUsed;
        TimesSuccessful = timesSuccessful;
        LastUpdatedAt = DateTime.UtcNow;

        // If used 3+ times and success rate < 30%, mark for retirement
        if (TimesUsed >= 3 && (TimesSuccessful / (decimal)TimesUsed) < 0.3m)
        {
            ShouldBeRetired = true;
        }
    }
}
