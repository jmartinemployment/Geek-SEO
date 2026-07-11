using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Mapping;

/// <summary>Round-trip persisted <see cref="SeoUrlResearch"/> for status-only updates (e.g. step 10 finalize).</summary>
public static class UrlResearchEntityMapper
{
    public static UrlResearchFullWrite ToFullWrite(
        SeoUrlResearch entity,
        string status,
        string dataQuality,
        DateTimeOffset? researchedAt = null)
    {
        return new UrlResearchFullWrite
        {
            DerivedKeyword = entity.DerivedKeyword,
            SearchLocation = entity.SearchLocation,
            BusinessContext = entity.BusinessContext,
            GbpSource = entity.GbpSource,
            Status = status,
            ErrorMessage = entity.ErrorMessage,
            DataQuality = dataQuality,
            DataQualityNotes = entity.DataQualityNotes,
            IntentPrimary = entity.IntentPrimary,
            IntentJustification = entity.IntentJustification,
            PafType = entity.PafType,
            PafFormat = entity.PafFormat,
            PafText = entity.PafText,
            PafSourceUrl = entity.PafSourceUrl,
            PafBeatStrategy = entity.PafBeatStrategy,
            DirectAnswerInstruction = entity.DirectAnswerInstruction,
            MustBeatPaf = entity.MustBeatPaf,
            MedianWordCountTop5 = entity.MedianWordCountTop5,
            MedianTitleLengthTop10 = entity.MedianTitleLengthTop10,
            MedianH2CountTop5 = entity.MedianH2CountTop5,
            DominantContentFormat = entity.DominantContentFormat,
            ResearchedAt = researchedAt ?? entity.ResearchedAt,
            Organic = entity.OrganicResults.Select(o => new UrlResearchOrganicWrite
            {
                Position = o.Position,
                Url = o.Url,
                Domain = o.Domain,
                Title = o.Title,
                Snippet = o.Snippet,
                ContentType = o.ContentType,
            }).ToList(),
            PeopleAlsoAsk = entity.PeopleAlsoAsk.Select((p, i) => new UrlResearchPaaWrite
            {
                Question = p.Question,
                SerpAnswerPreview = p.SerpAnswerPreview,
                Depth = p.Depth,
                DisplayOrder = p.DisplayOrder != 0 ? p.DisplayOrder : i,
            }).ToList(),
            RelatedSearches = entity.RelatedSearches.Select((p, i) => new UrlResearchPasfWrite
            {
                SearchText = p.SearchText,
                DisplayOrder = p.DisplayOrder != 0 ? p.DisplayOrder : i,
            }).ToList(),
            Competitors = entity.Competitors.Select(c => new UrlResearchCompetitorWrite
            {
                Url = c.Url,
                Position = c.Position,
                H1 = c.H1,
                EstimatedWordCount = c.EstimatedWordCount,
                Headings = (c.Headings ?? [])
                    .OrderBy(h => h.DisplayOrder)
                    .Select(h => new UrlResearchHeadingWrite
                    {
                        Level = h.Level,
                        Text = h.Text,
                        DisplayOrder = h.DisplayOrder,
                    }).ToList(),
            }).ToList(),
            SourceHeadings = entity.SourceHeadings.Select((h, i) => new UrlResearchSourceHeadingWrite
            {
                Level = h.Level,
                Text = h.Text,
                DisplayOrder = h.DisplayOrder != 0 ? h.DisplayOrder : i,
            }).ToList(),
            RecommendedTerms = entity.RecommendedTerms.Select((t, i) => new UrlResearchTermWrite
            {
                Term = t.Term,
                DisplayOrder = t.DisplayOrder != 0 ? t.DisplayOrder : i,
            }).ToList(),
            ClosingFaqs = entity.ClosingFaqs.Select((f, i) => new UrlResearchClosingFaqWrite
            {
                Question = f.Question,
                Source = f.Source,
                DisplayOrder = f.DisplayOrder != 0 ? f.DisplayOrder : i,
            }).ToList(),
            SectionHints = entity.SectionHints.Select((h, i) => new UrlResearchSectionHintWrite
            {
                DisplayOrder = h.DisplayOrder != 0 ? h.DisplayOrder : i,
                Movement = h.Movement,
                Label = h.Label,
                SuggestedH2 = h.SuggestedH2,
                SubtopicsFromSerp = (h.SubtopicsFromSerp ?? []).ToList(),
            }).ToList(),
        };
    }
}
