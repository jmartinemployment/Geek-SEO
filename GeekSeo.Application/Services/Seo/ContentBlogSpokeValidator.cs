using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentBlogSpokeValidator
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "are", "be", "as", "into", "how",
        "what", "why", "when", "where", "who", "which", "that", "this", "these",
        "those", "do", "does", "will", "would", "should", "could", "have", "has",
        "had", "not", "via", "vs", "using",
    };

    public static ContentBlogSpokeValidationResult Validate(string pillarKeyword, ContentBlogSpoke spoke)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(spoke.Title))
            errors.Add("Blog title is required.");
        if (string.IsNullOrWhiteSpace(spoke.Slug))
            errors.Add("Blog slug is required.");
        if (string.IsNullOrWhiteSpace(spoke.PrimaryKeyword))
            errors.Add("Blog primary keyword is required.");
        if (string.IsNullOrWhiteSpace(spoke.ContentHtml) || spoke.ContentHtml.Length < 200)
            errors.Add("Blog body is required.");

        ValidateKeywordCollision(pillarKeyword, spoke.PrimaryKeyword, errors);

        return new ContentBlogSpokeValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
        };
    }

    private static void ValidateKeywordCollision(string pillarKeyword, string? spokeKeyword, List<string> errors)
    {
        var pillar = pillarKeyword?.Trim();
        var spoke = spokeKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(pillar) || string.IsNullOrWhiteSpace(spoke))
            return;

        var pillarNorm = NormalizeKeyword(pillar);
        var spokeNorm = NormalizeKeyword(spoke);
        if (pillarNorm.Length == 0 || spokeNorm.Length == 0)
            return;

        if (string.Equals(pillarNorm, spokeNorm, StringComparison.Ordinal))
        {
            errors.Add($"Blog keyword collides with pillar keyword: \"{pillar}\".");
            return;
        }

        if (pillarNorm.Contains(spokeNorm, StringComparison.Ordinal) ||
            spokeNorm.Contains(pillarNorm, StringComparison.Ordinal))
        {
            errors.Add($"Blog keyword is a substring collision with pillar: \"{pillar}\" / \"{spoke}\".");
        }
    }

    public static string NormalizeKeyword(string keyword) =>
        string.Join(' ',
            keyword.ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !Stopwords.Contains(w)));
}
