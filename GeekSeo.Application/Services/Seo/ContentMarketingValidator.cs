using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentMarketingValidator
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "are", "be", "as", "into", "how",
        "what", "why", "when", "where", "who", "which", "that", "this", "these",
        "those", "do", "does", "will", "would", "should", "could", "have", "has",
        "had", "not", "via", "vs", "using",
    };

    public static ContentMarketingValidationResult Validate(ContentMarketingBundle bundle)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(bundle.DepartmentSlug))
            errors.Add("departmentSlug is required.");
        if (string.IsNullOrWhiteSpace(bundle.UseCaseSlug))
            errors.Add("useCaseSlug is required.");
        if (string.IsNullOrWhiteSpace(bundle.PrimaryKeyword))
            errors.Add("primaryKeyword is required.");

        ValidateDistinctSummaries(bundle, errors);
        ValidateKeywordCollision(bundle, errors);
        ValidateSocial(bundle, errors);

        return new ContentMarketingValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }

    private static void ValidateDistinctSummaries(ContentMarketingBundle bundle, List<string> errors)
    {
        var home = bundle.HomeSummary?.Trim();
        var hub = bundle.HubSummary?.Trim();
        var meta = bundle.MetaDescription?.Trim();

        if (string.IsNullOrWhiteSpace(home))
            errors.Add("homeSummary is required.");
        if (string.IsNullOrWhiteSpace(hub))
            errors.Add("hubSummary is required.");
        if (string.IsNullOrWhiteSpace(meta))
            errors.Add("metaDescription is required.");

        if (home is null || hub is null || meta is null)
            return;

        if (string.Equals(home, hub, StringComparison.Ordinal))
            errors.Add("homeSummary and hubSummary must be distinct.");
        if (string.Equals(home, meta, StringComparison.Ordinal))
            errors.Add("homeSummary and metaDescription must be distinct.");
        if (string.Equals(hub, meta, StringComparison.Ordinal))
            errors.Add("hubSummary and metaDescription must be distinct.");
    }

    private static void ValidateKeywordCollision(ContentMarketingBundle bundle, List<string> errors)
    {
        var pillar = bundle.PrimaryKeyword?.Trim();
        var spoke = bundle.BlogSpoke?.PrimaryKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(pillar) || string.IsNullOrWhiteSpace(spoke))
            return;

        var pillarNorm = NormalizeKeyword(pillar);
        var spokeNorm = NormalizeKeyword(spoke);
        if (pillarNorm.Length == 0 || spokeNorm.Length == 0)
            return;

        if (string.Equals(pillarNorm, spokeNorm, StringComparison.Ordinal))
        {
            errors.Add($"Blog spoke keyword collides with pillar keyword: \"{pillar}\".");
            return;
        }

        if (pillarNorm.Contains(spokeNorm, StringComparison.Ordinal) ||
            spokeNorm.Contains(pillarNorm, StringComparison.Ordinal))
        {
            errors.Add($"Blog spoke keyword is a substring collision with pillar: \"{pillar}\" / \"{spoke}\".");
        }
    }

    private static void ValidateSocial(ContentMarketingBundle bundle, List<string> errors)
    {
        var social = bundle.Social;
        if (social is null)
            return;

        if (social.LinkedIn is not null && social.Facebook is not null &&
            string.Equals(social.LinkedIn.Body.Trim(), social.Facebook.Body.Trim(), StringComparison.Ordinal))
        {
            errors.Add("LinkedIn and Facebook posts must be distinct.");
        }
    }

    public static string NormalizeKeyword(string keyword) =>
        string.Join(' ',
            keyword.ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !Stopwords.Contains(w)));
}
