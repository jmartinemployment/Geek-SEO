using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Mapping;

public static class WritingResearchContextMapper
{
    public static WritingResearchContext FromEntity(SeoUrlResearch row)
    {
        var competitors = row.Competitors
            .OrderBy(c => c.Position)
            .Select(c => new WritingResearchCompetitor
            {
                Url = c.Url,
                Position = c.Position,
                H1 = c.H1,
                EstimatedWordCount = c.EstimatedWordCount,
                Headings = c.Headings
                    .OrderBy(h => h.DisplayOrder)
                    .Select(h => new WritingResearchHeading
                    {
                        Level = h.Level,
                        Text = h.Text,
                        DisplayOrder = h.DisplayOrder,
                    })
                    .ToList(),
            })
            .ToList();

        return new WritingResearchContext
        {
            UrlResearchId = row.Id,
            ProjectId = row.ProjectId,
            UserId = row.UserId,
            SourceUrl = row.SourceUrl,
            DerivedKeyword = row.DerivedKeyword,
            SearchLocation = row.SearchLocation,
            BusinessContext = row.BusinessContext,
            IntentPrimary = row.IntentPrimary,
            IntentJustification = row.IntentJustification,
            Paf = new WritingResearchPaf
            {
                Type = row.PafType,
                Format = row.PafFormat,
                Text = row.PafText,
                SourceUrl = row.PafSourceUrl,
                BeatStrategy = row.PafBeatStrategy,
            },
            DirectAnswerInstruction = row.DirectAnswerInstruction,
            MustBeatPaf = row.MustBeatPaf,
            Benchmarks = new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = row.MedianWordCountTop5,
                MedianTitleLengthTop10 = row.MedianTitleLengthTop10,
                MedianH2CountTop5 = row.MedianH2CountTop5,
                DominantContentFormat = row.DominantContentFormat,
            },
            DataQuality = row.DataQuality,
            DataQualityNotes = row.DataQualityNotes,
            ResearchedAt = row.ResearchedAt,
            Organic = row.OrganicResults
                .OrderBy(o => o.Position)
                .Select(o => new WritingResearchOrganic
                {
                    Position = o.Position,
                    Url = o.Url,
                    Domain = o.Domain,
                    Title = o.Title,
                    Snippet = o.Snippet,
                    ContentType = o.ContentType,
                })
                .ToList(),
            PeopleAlsoAsk = row.PeopleAlsoAsk
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new WritingResearchPaa
                {
                    Question = p.Question,
                    SerpAnswerPreview = p.SerpAnswerPreview,
                    Depth = p.Depth,
                    DisplayOrder = p.DisplayOrder,
                })
                .ToList(),
            RelatedSearches = row.RelatedSearches
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new WritingResearchPasf
                {
                    SearchText = p.SearchText,
                    DisplayOrder = p.DisplayOrder,
                })
                .ToList(),
            Competitors = competitors,
            SourceHeadings = row.SourceHeadings
                .OrderBy(h => h.DisplayOrder)
                .Select(h => new WritingResearchHeading
                {
                    Level = h.Level,
                    Text = h.Text,
                    DisplayOrder = h.DisplayOrder,
                })
                .ToList(),
            RecommendedTerms = row.RecommendedTerms
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new WritingResearchTerm
                {
                    Term = t.Term,
                    DisplayOrder = t.DisplayOrder,
                })
                .ToList(),
            ClosingFaqs = row.ClosingFaqs
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new WritingResearchClosingFaq
                {
                    Question = f.Question,
                    Source = f.Source,
                    DisplayOrder = f.DisplayOrder,
                })
                .ToList(),
            SectionHints = row.SectionHints
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new WritingResearchSectionHint
                {
                    DisplayOrder = s.DisplayOrder,
                    Movement = s.Movement,
                    Label = s.Label,
                    SuggestedH2 = s.SuggestedH2,
                    SubtopicsFromSerp = s.SubtopicsFromSerp,
                })
                .ToList(),
        };
    }
}
