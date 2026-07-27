using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IContentIntelligenceValidator
{
    ContentValidationResult ValidateAgainstSiteContext(ContentAssetVersion draft, SiteAudit siteAudit, StrategyBrief brief);
}

public class ContentIntelligenceValidator : IContentIntelligenceValidator
{
    public ContentValidationResult ValidateAgainstSiteContext(ContentAssetVersion draft, SiteAudit siteAudit, StrategyBrief brief)
    {
        var warnings = new List<string>();
        var recommendations = new List<string>();

        // 1. Check for redundancy with existing content
        var redundancyIssues = CheckForRedundancy(draft, siteAudit);
        warnings.AddRange(redundancyIssues);

        // 2. Check positioning alignment
        var positioningIssues = CheckPositioningAlignment(brief, siteAudit);
        warnings.AddRange(positioningIssues);

        // 3. Check for missing references to existing content
        var missingReferences = CheckMissingReferences(draft, siteAudit);
        recommendations.AddRange(missingReferences);

        // 4. Check offering alignment
        var offeringIssues = CheckOfferingAlignment(draft, siteAudit);
        warnings.AddRange(offeringIssues);

        // 5. Check audience segment specificity
        var audienceIssues = CheckAudienceSpecificity(brief, siteAudit);
        recommendations.AddRange(audienceIssues);

        return new ContentValidationResult
        {
            IsValid = warnings.Count == 0,
            Warnings = warnings,
            Recommendations = recommendations,
            SiteContextConsidered = true
        };
    }

    private List<string> CheckForRedundancy(ContentAssetVersion draft, SiteAudit siteAudit)
    {
        var warnings = new List<string>();

        // Check if any existing content covers the same primary topic too deeply
        // This is a simplified check — in practice, use semantic similarity
        var documentText = draft.BodyDocumentJson.ToLower();

        foreach (var node in siteAudit.ContentInventory)
        {
            // If existing content is cornerstone on same topic and this is not substantially different
            if (node.IsCornerstoneContent && node.Type == ContentNodeType.Cornerstone)
            {
                // Flag if keyword overlap is high
                if (!string.IsNullOrEmpty(node.PrimaryKeyword) && documentText.Contains(node.PrimaryKeyword.ToLower()))
                {
                    warnings.Add($"Possible redundancy with existing cornerstone: {node.Title} ({node.Url})");
                }
            }
        }

        return warnings;
    }

    private List<string> CheckPositioningAlignment(StrategyBrief brief, SiteAudit siteAudit)
    {
        var warnings = new List<string>();

        // Check if positioning matches their stated positioning
        var sitePositioning = siteAudit.PositioningSummary.ToLower();

        // If this is a Cornerstone piece, ensure it's comprehensive enough
        // If it's a Differentiator, ensure it actually differentiates from competitors

        // Simplified: just flag if we should reconsider
        if (string.IsNullOrEmpty(brief.Angle))
        {
            warnings.Add("Angle is not clearly defined — content positioning may drift");
        }

        return warnings;
    }

    private List<string> CheckMissingReferences(ContentAssetVersion draft, SiteAudit siteAudit)
    {
        var recommendations = new List<string>();

        // Identify related cornerstone or pillar content that should be referenced
        var relatedContent = siteAudit.TopicalClusters
            .Where(c => draft.BodyDocumentJson.Contains(c.Topic, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var cluster in relatedContent)
        {
            if (!string.IsNullOrEmpty(cluster.CornerstonePage))
            {
                recommendations.Add($"Consider linking to cornerstone content on {cluster.Topic}: {cluster.CornerstonePage}");
            }
        }

        return recommendations;
    }

    private List<string> CheckOfferingAlignment(ContentAssetVersion draft, SiteAudit siteAudit)
    {
        var warnings = new List<string>();

        // Check if content references their actual offerings
        var documentText = draft.BodyDocumentJson.ToLower();
        var offeringsCovered = 0;

        foreach (var offering in siteAudit.PrimaryOfferingsJson)
        {
            if (documentText.Contains(offering.ToLower()))
            {
                offeringsCovered++;
            }
        }

        // If no offerings mentioned and this is service-related content, flag it
        if (offeringsCovered == 0 && siteAudit.PrimaryOfferingsJson.Count > 0)
        {
            warnings.Add("Content doesn't mention any of the client's primary offerings — ensure CTAs are aligned");
        }

        return warnings;
    }

    private List<string> CheckAudienceSpecificity(StrategyBrief brief, SiteAudit siteAudit)
    {
        var recommendations = new List<string>();

        // Check if audience is specific or generic
        if (brief.AudienceProfile.ToLower().Contains("all") || brief.AudienceProfile.ToLower().Contains("everyone"))
        {
            recommendations.Add("Audience is very broad — consider targeting specific segment from: " +
                string.Join(", ", siteAudit.AudienceSegmentsJson));
        }

        return recommendations;
    }
}

public class ContentValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public bool SiteContextConsidered { get; set; }
}
