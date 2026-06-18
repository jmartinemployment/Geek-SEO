using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class SerpFeatureGuidanceBuilder
{
    public static IReadOnlyList<SerpFeatureGuidance> Build(SerpFeatures features)
    {
        var list = new List<SerpFeatureGuidance>();
        if (features.HasFeaturedSnippet)
        {
            list.Add(new SerpFeatureGuidance
            {
                Feature = "featured_snippet",
                SuggestionId = "serp_featured_snippet",
                ApplyMode = "deterministic",
                ActionText =
                    "Featured snippet detected. Add a 40 to 60 word direct answer in a paragraph immediately after your first H2.",
            });
        }
        if (features.HasLocalPack)
        {
            list.Add(new SerpFeatureGuidance
            {
                Feature = "local_pack",
                ActionText =
                    "Local pack detected. Include city or region in the title and add NAP or service area details.",
            });
        }
        if (features.HasImagePack)
        {
            list.Add(new SerpFeatureGuidance
            {
                Feature = "image_pack",
                ActionText = "Image pack detected. Add descriptive alt text on 2 or more relevant images.",
            });
        }
        if (features.HasAiOverview)
        {
            list.Add(new SerpFeatureGuidance
            {
                Feature = "ai_overview",
                ActionText =
                    "Google AI Overview detected. Lead with a concise definition, cite authoritative sources, and structure content for extractable answers.",
            });
        }
        return list;
    }
}
