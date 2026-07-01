using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class SourceDiscoveryService(IAIProvider ai, INicheProfileRepository nicheProfiles) : ISourceDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<IReadOnlyList<DiscoveredSource>>> DiscoverAsync(
        Guid projectId,
        string keyword,
        string location,
        string plainTextExcerpt,
        CancellationToken ct = default)
    {
        var nicheContext = await BuildNicheContextAsync(projectId, keyword, ct);
        var excerpt = TrimExcerpt(plainTextExcerpt, maxWords: 800);

        var optimized = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                """
                You are a research assistant for SEO content. Return ONLY a JSON array of exactly 3 authoritative external sources relevant to the topic.
                Each item must have: url (absolute https URL), title (publisher or page title), anchorText (short link label).
                Prefer .gov, .edu, .mil, WHO, NIH, CDC, NIST, and other primary research or government sources.
                Do not include competitor SEO blogs, listicles, Reddit, Quora, Medium, or generic search pages.
                Do not invent people, quotes, or credentials. URLs must be real publisher homepages or article pages.
                No markdown fences or commentary.
                """,
            UserPrompt =
                $"""
                Keyword: {keyword.Trim()}
                Location: {location.Trim()}
                Niche context: {nicheContext}
                Article excerpt:
                {excerpt}
                """,
            MaxTokens = 1024,
            Temperature = 0.3,
        }, ct);

        if (!optimized.IsSuccess || optimized.Value is null)
            return Result<IReadOnlyList<DiscoveredSource>>.Failure(optimized.Error ?? "Could not discover sources with AI");

        var parsed = ParseDiscoveredSources(optimized.Value.Content);
        if (parsed.Count == 0)
            return Result<IReadOnlyList<DiscoveredSource>>.Failure("AI returned no usable source URLs. Try again or add sources manually.");

        return Result<IReadOnlyList<DiscoveredSource>>.Success(parsed);
    }

    public static IReadOnlyList<DiscoveredSource> ParseDiscoveredSources(string raw)
    {
        var json = ExtractJsonArray(raw);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<DiscoveredSourceDto>>(json, JsonOptions) ?? [];
            return items
                .Where(item => !string.IsNullOrWhiteSpace(item.Url) && !string.IsNullOrWhiteSpace(item.Title))
                .Where(item => AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(item.Url!))
                .Select(item => new DiscoveredSource
                {
                    Url = item.Url!.Trim(),
                    Title = item.Title!.Trim(),
                    AnchorText = string.IsNullOrWhiteSpace(item.AnchorText) ? item.Title!.Trim() : item.AnchorText.Trim(),
                })
                .Take(3)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<string> BuildNicheContextAsync(Guid projectId, string keyword, CancellationToken ct)
    {
        var profileResult = await nicheProfiles.GetLatestByProjectAsync(projectId, ct);
        var profile = profileResult.IsSuccess ? profileResult.Value : null;
        if (profile is null)
            return "No niche profile available.";

        var pillar = FindMatchedPillar(keyword, profile);
        var tags = profile.NicheTags.Length > 0 ? string.Join(", ", profile.NicheTags.Take(6)) : "none";
        return $"Primary niche: {profile.PrimaryNiche}. Description: {profile.NicheDescription}. Tags: {tags}. Matched pillar: {pillar ?? "none"}.";
    }

    private static string? FindMatchedPillar(string keyword, NicheProfile profile)
    {
        if (profile.Pillars is null || profile.Pillars.Count == 0)
            return null;

        var normalizedKeyword = keyword.ToLowerInvariant();
        var direct = profile.Pillars.FirstOrDefault(p =>
            normalizedKeyword.Contains(p.PillarTopic, StringComparison.OrdinalIgnoreCase)
            || normalizedKeyword.Contains(p.PrimaryKeyword, StringComparison.OrdinalIgnoreCase));

        return direct?.PillarTopic ?? profile.Pillars.First().PillarTopic;
    }

    private static string TrimExcerpt(string plainText, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return "No article excerpt available.";

        var words = plainText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords)
            return plainText.Trim();

        return string.Join(' ', words.Take(maxWords)).Trim() + "…";
    }

    private static string ExtractJsonArray(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = Regex.Replace(trimmed, "^```(?:json)?\\s*", "", RegexOptions.IgnoreCase).Trim();
            trimmed = Regex.Replace(trimmed, "\\s*```$", "").Trim();
        }

        var start = trimmed.IndexOf('[');
        var end = trimmed.LastIndexOf(']');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }

    private sealed class DiscoveredSourceDto
    {
        public string? Url { get; init; }
        public string? Title { get; init; }
        public string? AnchorText { get; init; }
    }
}
