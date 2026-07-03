using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ResearchDraftIdentityPrompt
{
    public static string BuildIdentityLine(WritingResearchContext research, BusinessVoicePack pack)
    {
        if (!pack.Enabled)
            return string.Empty;

        var siteName = ResolveSiteName(research, pack);
        var keyword = ResolveKeyword(research);
        var geoClause = BuildGeoClause(research);

        return
            $"You are {siteName}, an IT consultancy specializing in implementing AI for {keyword}{geoClause} " +
            "Write as the implementer who designs and deploys for SMB clients — not a national category blog or vendor listicle.";
    }

    public static VoicePackDiagnostics Diagnose(WritingResearchContext research)
    {
        var pack = BusinessVoicePackBuilder.Build(research);
        var userPrompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = research,
            Title = research.DerivedKeyword,
        });

        return new VoicePackDiagnostics(
            pack.Enabled,
            pack.IsImplementationConsultancy,
            !string.IsNullOrWhiteSpace(research.SiteFocus?.SiteName),
            !string.IsNullOrWhiteSpace(research.DerivedKeyword),
            userPrompt.Contains("Business voice pack", StringComparison.OrdinalIgnoreCase),
            KeywordWritingFamilyCatalog.DetectFamilyId(
                research.DerivedKeyword,
                research.RecommendedTerms.Select(t => t.Term)));
    }

    private static string ResolveSiteName(WritingResearchContext research, BusinessVoicePack pack)
    {
        if (!string.IsNullOrWhiteSpace(pack.SiteName))
            return pack.SiteName.Trim();

        if (!string.IsNullOrWhiteSpace(research.BusinessContext))
        {
            var firstSentence = research.BusinessContext
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstSentence))
                return firstSentence.Trim();
        }

        return "the authoring business";
    }

    private static string ResolveKeyword(WritingResearchContext research)
    {
        if (!string.IsNullOrWhiteSpace(research.DerivedKeyword))
            return research.DerivedKeyword.Trim();

        if (!string.IsNullOrWhiteSpace(research.SerpKeyword))
            return research.SerpKeyword.Trim();

        return "this topic";
    }

    private static string BuildGeoClause(WritingResearchContext research)
    {
        var focus = research.SiteFocus;
        string? geo = null;

        if (!string.IsNullOrWhiteSpace(focus?.ServiceAreaDescription))
            geo = focus.ServiceAreaDescription.Trim();
        else if (focus?.GeoAnchorNodes.Count > 0)
            geo = string.Join("; ", focus.GeoAnchorNodes.Take(3));
        else if (!string.IsNullOrWhiteSpace(research.SearchLocation)
            && !research.SearchLocation.Equals("United States", StringComparison.OrdinalIgnoreCase))
        {
            geo = research.SearchLocation.Trim();
        }

        return string.IsNullOrWhiteSpace(geo)
            ? " for SMB buyers."
            : $" for SMB buyers in {geo}.";
    }
}

public sealed record VoicePackDiagnostics(
    bool VoicePackEnabled,
    bool IsImplementationConsultancy,
    bool HasSiteName,
    bool HasKeyword,
    bool UserPromptIncludesVoicePack,
    string KeywordFamilyId);
