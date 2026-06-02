using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Mapping;

public static class NicheBulkInsertMapper
{
    public static NichePillarBulkInsert ToBulkInsert(NichePillar pillar) =>
        new(
            pillar.Id,
            pillar.NicheProfileId,
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

    public static NichePillar ToEntity(NichePillarBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            NicheProfileId = dto.NicheProfileId,
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

    public static NicheSubtopicBulkInsert ToBulkInsert(NicheSubtopic subtopic) =>
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

    public static NicheSubtopic ToEntity(NicheSubtopicBulkInsert dto) =>
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

    public static NicheCompetitorBulkInsert ToBulkInsert(NicheCompetitor competitor) =>
        new(
            competitor.Id,
            competitor.NicheProfileId,
            competitor.Domain,
            competitor.SerpPresence,
            competitor.EstimatedAuthorityScore,
            competitor.PillarsRanking,
            competitor.StrengthAssessment);

    public static NicheCompetitor ToEntity(NicheCompetitorBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            NicheProfileId = dto.NicheProfileId,
            Domain = dto.Domain,
            SerpPresence = dto.SerpPresence,
            EstimatedAuthorityScore = dto.EstimatedAuthorityScore,
            PillarsRanking = dto.PillarsRanking,
            StrengthAssessment = dto.StrengthAssessment,
        };

    public static NicheEntityBulkInsert ToBulkInsert(NicheEntity entity) =>
        new(
            entity.Id,
            entity.NicheProfileId,
            entity.EntityName,
            entity.EntityType,
            entity.MentionFrequency,
            entity.PresentOnDomain,
            entity.AssociatedPillarIds);

    public static NicheEntity ToEntity(NicheEntityBulkInsert dto) =>
        new()
        {
            Id = dto.Id,
            NicheProfileId = dto.NicheProfileId,
            EntityName = dto.EntityName,
            EntityType = dto.EntityType,
            MentionFrequency = dto.MentionFrequency,
            PresentOnDomain = dto.PresentOnDomain,
            AssociatedPillarIds = dto.AssociatedPillarIds,
        };

    public static NichePillarPageBulkInsert ToBulkInsert(NichePillarPage page) =>
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

    public static NichePillarPage ToEntity(NichePillarPageBulkInsert dto) =>
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
