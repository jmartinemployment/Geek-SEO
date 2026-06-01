using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class InternalKeywordDiscoveryProvider : IKeywordDiscoveryProvider
{
    public string ProviderName => "internal";

    public Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default)
    {
        var seedTokens = TokenizeKeyword(seedKeyword);
        if (seedTokens.Count == 0)
            return Task.FromResult(Result<IReadOnlyList<KeywordResult>>.Success([]));

        var modifiers = new[] { "best", "top", "how to", "what is", "why", "guide", "tutorial", "tools", "free", "cheap", "cost of" };
        var results = new List<KeywordResult>();

        foreach (var modifier in modifiers)
        {
            if (results.Count >= count) break;

            var expanded = $"{modifier} {seedKeyword}";
            results.Add(new KeywordResult
            {
                Keyword = expanded,
                SearchVolume = 0,
                KeywordDifficulty = 0,
                CpcUsd = 0,
                Competition = "Unknown",
            });

            if (results.Count >= count) break;
            var reversed = $"{seedKeyword} {modifier}";
            results.Add(new KeywordResult
            {
                Keyword = reversed,
                SearchVolume = 0,
                KeywordDifficulty = 0,
                CpcUsd = 0,
                Competition = "Unknown",
            });
        }

        return Task.FromResult(Result<IReadOnlyList<KeywordResult>>.Success(results.Take(count).ToList()));
    }

    private static List<string> TokenizeKeyword(string keyword)
    {
        return Regex.Split(keyword.ToLowerInvariant(), @"\W+")
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 2)
            .Distinct()
            .ToList();
    }

    private static string GetCompetitionLabel(double difficulty)
    {
        return difficulty switch
        {
            < 10 => "Low",
            < 30 => "Medium",
            < 60 => "High",
            _ => "Very High",
        };
    }
}
