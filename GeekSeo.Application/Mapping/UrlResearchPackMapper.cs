using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Mapping;

public static class UrlResearchPackMapper
{
    public static UrlResearchFullWrite ToFullWrite(SerpResearchPack pack, string status = "completed")
    {
        var researchedAt = DateTimeOffset.TryParse(pack.Meta.ResearchedAt, out var parsed)
            ? parsed
            : DateTimeOffset.UtcNow;

        var h2Counts = pack.CompetitorOutlines
            .Select(c => c.Headings.Count(h => h.Level == 2))
            .OrderBy(n => n)
            .ToList();
        var medianH2 = h2Counts.Count > 0 ? h2Counts[h2Counts.Count / 2] : 0;

        return new UrlResearchFullWrite
        {
            DerivedKeyword = pack.Meta.Keyword,
            SearchLocation = pack.Meta.Location,
            BusinessContext = pack.Meta.BusinessContext,
            GbpSource = "none",
            Status = status,
            DataQuality = MapDataQuality(pack.Meta.DataQuality),
            DataQualityNotes = pack.Meta.Notes.Count > 0
                ? string.Join(" ", pack.Meta.Notes)
                : null,
            IntentPrimary = pack.Intent.Primary,
            IntentJustification = pack.Intent.Justification,
            PafType = pack.Paf.Type,
            PafFormat = pack.Paf.Format,
            PafText = pack.Paf.Text,
            PafSourceUrl = pack.Paf.SourceUrl,
            PafBeatStrategy = pack.Paf.BeatStrategy,
            DirectAnswerInstruction = pack.DirectAnswerBlock.Instruction,
            MustBeatPaf = pack.DirectAnswerBlock.MustBeatPaf,
            MedianWordCountTop5 = pack.Benchmarks.MedianWordCountTop5,
            MedianTitleLengthTop10 = pack.Benchmarks.MedianTitleLengthTop10,
            MedianH2CountTop5 = medianH2,
            DominantContentFormat = pack.Benchmarks.DominantContentFormat,
            ResearchedAt = researchedAt,
            Organic = pack.Organic.Select((o, i) => new UrlResearchOrganicWrite
            {
                Position = o.Position,
                Url = o.Url,
                Domain = o.Domain,
                Title = o.Title,
                Snippet = o.Snippet,
                ContentType = o.ContentType,
            }).ToList(),
            PeopleAlsoAsk = pack.Paa.Select((p, i) => new UrlResearchPaaWrite
            {
                Question = p.Question,
                SerpAnswerPreview = p.SerpAnswerPreview,
                Depth = p.Depth,
                DisplayOrder = i,
            }).ToList(),
            RelatedSearches = pack.Pasf.Select((p, i) => new UrlResearchPasfWrite
            {
                SearchText = p,
                DisplayOrder = i,
            }).ToList(),
            Competitors = pack.CompetitorOutlines.Select(c => new UrlResearchCompetitorWrite
            {
                Url = c.Url,
                Position = c.Position,
                H1 = c.H1,
                EstimatedWordCount = c.EstimatedWordCount,
                Headings = c.Headings.Select((h, i) => new UrlResearchHeadingWrite
                {
                    Level = h.Level,
                    Text = h.Text,
                    DisplayOrder = i,
                }).ToList(),
            }).ToList(),
            SourceHeadings = pack.SourceHeadings.Select((h, i) => new UrlResearchSourceHeadingWrite
            {
                Level = h.Level,
                Text = h.Text,
                DisplayOrder = i,
            }).ToList(),
            RecommendedTerms = pack.RecommendedTerms.Select((t, i) => new UrlResearchTermWrite
            {
                Term = t,
                DisplayOrder = i,
            }).ToList(),
            ClosingFaqs = pack.ClosingFaqQuestions.Select((f, i) => new UrlResearchClosingFaqWrite
            {
                Question = f.Question,
                Source = f.Source,
                DisplayOrder = i,
            }).ToList(),
            SectionHints = pack.MethodologyHints.Select((h, i) => new UrlResearchSectionHintWrite
            {
                DisplayOrder = i,
                Movement = h.Movement,
                Label = h.Label,
                SuggestedH2 = h.SuggestedH2,
                SubtopicsFromSerp = h.SubtopicsFromSerp.ToList(),
            }).ToList(),
        };
    }

    private static string MapDataQuality(string packQuality) => packQuality switch
    {
        "live" => "full",
        "partial" => "partial",
        _ => "weak",
    };
}
