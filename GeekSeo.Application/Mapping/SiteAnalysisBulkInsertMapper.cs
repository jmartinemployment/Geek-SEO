using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Mapping;

public static class SiteAnalysisBulkInsertMapper
{
    public static SiteAnalysisPillarBulkInsert ToBulkInsert(SiteAnalysisPillar pillar) =>
        new(
            pillar.Id,
            pillar.SiteAnalysisProfileId,
            pillar.PillarTopic,
            pillar.PillarSlug,
            pillar.PrimaryKeyword,
            pillar.PageUrl,
            pillar.SearchIntent,
            pillar.SearchVolume,
            pillar.KeywordDifficulty,
            pillar.CoverageStatus,
            pillar.CoverageScore,
            pillar.ExistingPageCount,
            pillar.RequiredSubtopicCount,
            pillar.CoveredSubtopicCount,
            pillar.Priority,
            pillar.StrategicPriority,
            pillar.ContentAngle,
            pillar.EstimatedTrafficPotential,
            pillar.Source,
            pillar.DisplayOrder,
            pillar.CreatedAt);

    public static SiteAnalysisPillar ToEntity(SiteAnalysisPillarBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            SiteAnalysisProfileId = dto.SiteAnalysisProfileId,
            PillarTopic = dto.PillarTopic,
            PillarSlug = dto.PillarSlug,
            PrimaryKeyword = dto.PrimaryKeyword,
            PageUrl = dto.PageUrl,
            SearchIntent = dto.SearchIntent,
            SearchVolume = dto.SearchVolume,
            KeywordDifficulty = dto.KeywordDifficulty,
            CoverageStatus = dto.CoverageStatus,
            CoverageScore = dto.CoverageScore,
            ExistingPageCount = dto.ExistingPageCount,
            RequiredSubtopicCount = dto.RequiredSubtopicCount,
            CoveredSubtopicCount = dto.CoveredSubtopicCount,
            Priority = dto.Priority,
            StrategicPriority = dto.StrategicPriority,
            ContentAngle = dto.ContentAngle,
            EstimatedTrafficPotential = dto.EstimatedTrafficPotential,
            Source = dto.Source,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = dto.CreatedAt ?? DateTimeOffset.UtcNow,
        };

    public static SiteAnalysisSubtopicBulkInsert ToBulkInsert(SiteAnalysisSubtopic subtopic) =>
        new(
            subtopic.Id,
            subtopic.PillarId,
            subtopic.SubtopicTitle,
            subtopic.TargetKeyword,
            subtopic.SearchIntent,
            subtopic.SearchVolume,
            subtopic.KeywordDifficulty,
            subtopic.CoverageStatus,
            subtopic.ExistingUrl,
            subtopic.RecommendedFormat,
            subtopic.RecommendedWordCount,
            subtopic.FixEffort,
            subtopic.IsQuickWin,
            subtopic.CreatedAt);

    public static SiteAnalysisSubtopic ToEntity(SiteAnalysisSubtopicBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            PillarId = dto.PillarId,
            SubtopicTitle = dto.SubtopicTitle,
            TargetKeyword = dto.TargetKeyword,
            SearchIntent = dto.SearchIntent,
            SearchVolume = dto.SearchVolume,
            KeywordDifficulty = dto.KeywordDifficulty,
            CoverageStatus = dto.CoverageStatus,
            ExistingUrl = dto.ExistingUrl,
            RecommendedFormat = dto.RecommendedFormat,
            RecommendedWordCount = dto.RecommendedWordCount,
            FixEffort = dto.FixEffort,
            IsQuickWin = dto.IsQuickWin,
            CreatedAt = dto.CreatedAt ?? DateTimeOffset.UtcNow,
        };

    public static SiteAnalysisCompetitorBulkInsert ToBulkInsert(SiteAnalysisCompetitor competitor) =>
        new(
            competitor.Id,
            competitor.SiteAnalysisProfileId,
            competitor.Domain,
            competitor.SerpPresence,
            competitor.EstimatedAuthorityScore,
            competitor.PillarsRanking,
            competitor.StrengthAssessment,
            competitor.Scope,
            competitor.PagesCrawled,
            competitor.AvgWordCount,
            competitor.HasFaqSchema,
            competitor.ServicesJson,
            competitor.KnowsAboutJson,
            competitor.AreaServedJson,
            competitor.SameAsJson,
            competitor.Description,
            competitor.BrandName,
            competitor.PillarsJson,
            competitor.CompetitorAnalyzedAt);

    public static SiteAnalysisCompetitor ToEntity(SiteAnalysisCompetitorBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            SiteAnalysisProfileId = dto.SiteAnalysisProfileId,
            Domain = dto.Domain,
            SerpPresence = dto.SerpPresence,
            EstimatedAuthorityScore = dto.EstimatedAuthorityScore,
            PillarsRanking = dto.PillarsRanking,
            StrengthAssessment = dto.StrengthAssessment,
            Scope = dto.Scope,
            PagesCrawled = dto.PagesCrawled,
            AvgWordCount = dto.AvgWordCount,
            HasFaqSchema = dto.HasFaqSchema,
            ServicesJson = dto.ServicesJson,
            KnowsAboutJson = dto.KnowsAboutJson,
            AreaServedJson = dto.AreaServedJson,
            SameAsJson = dto.SameAsJson,
            Description = dto.Description,
            BrandName = dto.BrandName,
            PillarsJson = dto.PillarsJson,
            CompetitorAnalyzedAt = dto.CompetitorAnalyzedAt,
        };

    public static SiteAnalysisEntityBulkInsert ToBulkInsert(SiteAnalysisEntity entity) =>
        new(
            entity.Id,
            entity.SiteAnalysisProfileId,
            entity.EntityName,
            entity.EntityType,
            entity.MentionFrequency,
            entity.PresentOnDomain,
            entity.AssociatedPillarIds);

    public static SiteAnalysisEntity ToEntity(SiteAnalysisEntityBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            SiteAnalysisProfileId = dto.SiteAnalysisProfileId,
            EntityName = dto.EntityName,
            EntityType = dto.EntityType,
            MentionFrequency = dto.MentionFrequency,
            PresentOnDomain = dto.PresentOnDomain,
            AssociatedPillarIds = dto.AssociatedPillarIds,
        };

    public static SiteAnalysisPillarPageBulkInsert ToBulkInsert(SiteAnalysisPillarPage page) =>
        new(
            page.Id,
            page.PillarId,
            page.Url,
            page.PageTitle,
            page.WordCount,
            page.CoverageQuality,
            page.RelevanceScore,
            page.TopicsFound,
            page.GapsFound);

    public static SiteAnalysisPillarPage ToEntity(SiteAnalysisPillarPageBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            PillarId = dto.PillarId,
            Url = dto.Url,
            PageTitle = dto.PageTitle,
            WordCount = dto.WordCount,
            CoverageQuality = dto.CoverageQuality,
            RelevanceScore = dto.RelevanceScore,
            TopicsFound = dto.TopicsFound,
            GapsFound = dto.GapsFound,
        };
}
