using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Pure filter/score/rewrite pipeline for hub-and-spoke link planning (slice 4).
/// No persistence or API wiring — returns <see cref="ContentClusterPlanResult"/> for review/save in slice 5.
/// </summary>
public static partial class ContentClusterLinkPlanner
{
    private static readonly string[] FreeTierBusinessSignals =
    [
        "free",
        "freemium",
        "no cost",
        "no-cost",
        "trial",
        "open source",
        "open-source",
    ];

    public static ContentClusterPlanResult Plan(ContentClusterPlannerInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var pillarKeyword = input.PillarKeyword.Trim();
        var options = input.Options ?? new ContentClusterPlannerOptions();
        var businessContext = ResolveBusinessContext(input);
        var pillarNormalized = ContentBlogSpokeValidator.NormalizeKeyword(pillarKeyword);

        var filteredOut = new List<ContentClusterFilteredCandidate>();
        var scored = new List<ScoredCandidate>();

        foreach (var raw in CollectRawCandidates(input.Research))
        {
            var rejectReason = EvaluateRejectReason(raw.Phrase, pillarKeyword, pillarNormalized, businessContext, options);
            if (rejectReason is not null)
            {
                filteredOut.Add(new ContentClusterFilteredCandidate
                {
                    Phrase = raw.Phrase,
                    SourceType = raw.SourceType,
                    RejectReason = rejectReason,
                });
                continue;
            }

            scored.Add(new ScoredCandidate(
                raw.Phrase,
                raw.SourceType,
                raw.DisplayOrder,
                ScoreSpokeWorthiness(raw.Phrase, pillarNormalized)));
        }

        var spokeRanked = scored
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.DisplayOrder)
            .Take(options.MaxSpokeCandidates)
            .ToList();

        var spokePhrases = new HashSet<string>(
            spokeRanked.Select(c => c.Phrase),
            StringComparer.OrdinalIgnoreCase);

        var spokeCandidates = spokeRanked
            .Select(c => new ContentClusterCandidate
            {
                Phrase = c.Phrase,
                SourceType = c.SourceType,
                Score = c.Score,
                SuggestedQuestion = RewriteQuestion(c.Phrase, pillarKeyword),
                SuggestedSlug = ContentPublishSlug.NormalizeFromPhrase(c.Phrase),
            })
            .ToList();

        var faqItems = BuildFaqItems(
            pillarKeyword,
            scored,
            spokeCandidates,
            spokePhrases,
            options.MaxFaqItems);

        var bodyLinks = BuildBodyLinks(
            spokeCandidates,
            PillarH2Extractor.ExtractBodyHeadings(input.PillarContentHtml));

        return new ContentClusterPlanResult
        {
            SpokeCandidates = spokeCandidates,
            FaqItems = faqItems,
            BodyLinks = bodyLinks,
            FilteredOut = filteredOut,
        };
    }

    internal static IReadOnlyList<ContentLinkBodySlot> BuildBodyLinks(
        IReadOnlyList<ContentClusterCandidate> spokeCandidates,
        IReadOnlyList<PillarH2Heading> headings)
    {
        if (spokeCandidates.Count == 0 || headings.Count == 0)
            return [];

        var usedHints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slots = new List<ContentLinkBodySlot>();

        foreach (var spoke in spokeCandidates)
        {
            var hint = FindBestHeadingHint(spoke.Phrase, headings, usedHints);
            if (hint is null)
                continue;

            usedHints.Add(hint);
            var targetPath = string.IsNullOrWhiteSpace(spoke.SuggestedSlug)
                ? null
                : $"/blog/{spoke.SuggestedSlug}";

            slots.Add(new ContentLinkBodySlot
            {
                InsertAfterH2Hint = hint,
                TargetPath = targetPath,
                AnchorText = spoke.Phrase,
                Priority = slots.Count + 1,
            });
        }

        return slots;
    }

    private static string? FindBestHeadingHint(
        string spokePhrase,
        IReadOnlyList<PillarH2Heading> headings,
        ISet<string> usedHints)
    {
        var phraseTokens = Tokenize(spokePhrase);
        if (phraseTokens.Count == 0)
            return null;

        string? bestHint = null;
        var bestScore = 0.0;

        foreach (var heading in headings)
        {
            var hint = PillarH2Extractor.ResolveHint(heading);
            if (usedHints.Contains(hint))
                continue;

            var score = TokenOverlapRatio(phraseTokens, Tokenize(heading.Text));
            if (score > bestScore)
            {
                bestScore = score;
                bestHint = hint;
            }
        }

        if (bestHint is not null && bestScore > 0)
            return bestHint;

        return headings
            .Select(PillarH2Extractor.ResolveHint)
            .FirstOrDefault(h => !usedHints.Contains(h));
    }

    private static HashSet<string> Tokenize(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.Ordinal);

    private static double TokenOverlapRatio(ISet<string> left, ISet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;

        var overlap = left.Count(right.Contains);
        return (double)overlap / Math.Max(left.Count, right.Count);
    }

    private static IReadOnlyList<ContentLinkFaqItem> BuildFaqItems(
        string pillarKeyword,
        IReadOnlyList<ScoredCandidate> scored,
        IReadOnlyList<ContentClusterCandidate> spokeCandidates,
        HashSet<string> spokePhrases,
        int maxFaqItems)
    {
        var items = new List<ContentLinkFaqItem>();
        var seenQuestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string phrase, string sourceType, string? targetPath, string? anchorText)
        {
            if (items.Count >= maxFaqItems)
                return;

            var question = RewriteQuestion(phrase, pillarKeyword);
            if (!seenQuestions.Add(question))
                return;

            items.Add(new ContentLinkFaqItem
            {
                Question = question,
                Source = sourceType,
                TargetPath = targetPath,
                AnchorText = anchorText,
                LinkStatus = targetPath is null ? null : SpokeLinkStatuses.Planned,
            });
        }

        foreach (var spoke in spokeCandidates)
        {
            var targetPath = string.IsNullOrWhiteSpace(spoke.SuggestedSlug)
                ? null
                : $"/blog/{spoke.SuggestedSlug}";
            TryAdd(spoke.Phrase, spoke.SourceType, targetPath, spoke.Phrase);
        }

        foreach (var candidate in scored.OrderBy(c => c.DisplayOrder))
        {
            if (spokePhrases.Contains(candidate.Phrase))
                continue;

            TryAdd(candidate.Phrase, candidate.SourceType, null, null);
        }

        if (items.Count < maxFaqItems)
        {
            foreach (var fallback in ContentWritingRules.BuildClosingFaqQuestions(pillarKeyword, [], null))
            {
                if (items.Count >= maxFaqItems)
                    break;

                if (!seenQuestions.Add(fallback))
                    continue;

                items.Add(new ContentLinkFaqItem
                {
                    Question = fallback,
                    Source = "suggested",
                });
            }
        }

        return items.Take(maxFaqItems).ToList();
    }

    private static IEnumerable<RawCandidate> CollectRawCandidates(WritingResearchContext research)
    {
        var order = 0;

        foreach (var paa in research.PeopleAlsoAsk.OrderBy(p => p.DisplayOrder))
        {
            if (string.IsNullOrWhiteSpace(paa.Question))
                continue;

            yield return new RawCandidate(paa.Question.Trim(), SpokeSourceTypes.Paa, order++);
        }

        foreach (var pasf in research.RelatedSearches.OrderBy(p => p.DisplayOrder))
        {
            if (string.IsNullOrWhiteSpace(pasf.SearchText))
                continue;

            yield return new RawCandidate(pasf.SearchText.Trim(), SpokeSourceTypes.Pasf, order++);
        }
    }

    private static string? EvaluateRejectReason(
        string phrase,
        string pillarKeyword,
        string pillarNormalized,
        string businessContext,
        ContentClusterPlannerOptions options)
    {
        if (IsNearDuplicatePillarKeyword(phrase, pillarKeyword, pillarNormalized))
            return "near_duplicate_pillar_keyword";

        var lower = phrase.ToLowerInvariant();
        foreach (var blocked in options.IntentBlocklist)
        {
            if (ContainsToken(lower, blocked))
                return $"intent_blocklist:{blocked}";
        }

        if (ContainsToken(lower, "free") && !BusinessOffersFreeTier(businessContext))
            return "free_tier_mismatch";

        return null;
    }

    private static bool IsNearDuplicatePillarKeyword(string phrase, string pillarKeyword, string pillarNormalized)
    {
        var phraseNorm = ContentBlogSpokeValidator.NormalizeKeyword(phrase);
        if (phraseNorm.Length == 0 || pillarNormalized.Length == 0)
            return false;

        if (string.Equals(phraseNorm, pillarNormalized, StringComparison.Ordinal))
            return true;

        return pillarNormalized.Contains(phraseNorm, StringComparison.Ordinal) ||
               phraseNorm.Contains(pillarNormalized, StringComparison.Ordinal);
    }

    private static bool BusinessOffersFreeTier(string businessContext)
    {
        if (string.IsNullOrWhiteSpace(businessContext))
            return false;

        var lower = businessContext.ToLowerInvariant();
        if (lower.Contains("no free", StringComparison.Ordinal) ||
            lower.Contains("not free", StringComparison.Ordinal) ||
            lower.Contains("without free", StringComparison.Ordinal) ||
            lower.Contains("no freemium", StringComparison.Ordinal))
        {
            return false;
        }

        return FreeTierBusinessSignals.Any(signal => lower.Contains(signal, StringComparison.Ordinal));
    }

    private static double ScoreSpokeWorthiness(string phrase, string pillarNormalized)
    {
        var lower = phrase.ToLowerInvariant();
        var score = 1.0;

        if (BestPattern().IsMatch(lower))
            score += 3;
        if (lower.Contains("companies", StringComparison.Ordinal))
            score += 2;
        if (lower.Contains("how to", StringComparison.Ordinal) || lower.Contains("how-to", StringComparison.Ordinal))
            score += 2;
        if (lower.Contains(" vs ", StringComparison.Ordinal) || lower.Contains("compare", StringComparison.Ordinal))
            score += 2;

        var phraseNorm = ContentBlogSpokeValidator.NormalizeKeyword(phrase);
        var overlap = TokenOverlapRatio(pillarNormalized, phraseNorm);
        if (overlap >= 0.75 && !string.Equals(pillarNormalized, phraseNorm, StringComparison.Ordinal))
            score -= 2;

        return score;
    }

    public static string RewriteQuestion(string phrase, string pillarKeyword)
    {
        var trimmed = phrase.Trim();
        if (trimmed.Contains('?', StringComparison.Ordinal))
            return trimmed;

        var lower = trimmed.ToLowerInvariant();

        if (lower.Contains("companies", StringComparison.Ordinal))
            return $"Which companies offer {trimmed}?";

        if (BestPattern().IsMatch(lower))
        {
            var withoutBest = BestPattern().Replace(trimmed, string.Empty).Trim();
            withoutBest = Regex.Replace(withoutBest, @"\s+", " ").Trim();
            return $"Which {withoutBest} are best?";
        }

        if (ContainsToken(lower, "free"))
            return $"Are there free options for {trimmed}?";

        if (ContainsToken(lower, "cost") || ContainsToken(lower, "price") || ContainsToken(lower, "pricing"))
            return $"How much does {trimmed} cost?";

        if (ContainsToken(lower, "vs") || ContainsToken(lower, "versus") || ContainsToken(lower, "compare"))
            return $"How does {trimmed} compare to alternatives?";

        if (ContainsToken(lower, "how"))
            return $"How do you get started with {trimmed}?";

        return $"What are the key considerations for {trimmed}?";
    }

    private static string ResolveBusinessContext(ContentClusterPlannerInput input)
    {
        if (input.SiteFocus is not null)
        {
            var fromFocus = SiteWritingFocusSerializer.ToBusinessContext(input.SiteFocus);
            if (!string.IsNullOrWhiteSpace(fromFocus))
                return fromFocus;
        }

        return input.Research.BusinessContext?.Trim() ?? string.Empty;
    }

    private static bool ContainsToken(string lowerPhrase, string token) =>
        lowerPhrase.Contains(token, StringComparison.Ordinal);

    private static double TokenOverlapRatio(string left, string right)
    {
        var leftTokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rightTokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (leftTokens.Length == 0 || rightTokens.Length == 0)
            return 0;

        var rightSet = new HashSet<string>(rightTokens, StringComparer.Ordinal);
        var overlap = leftTokens.Count(rightSet.Contains);
        return (double)overlap / Math.Max(leftTokens.Length, rightTokens.Length);
    }

    [GeneratedRegex(@"\bbest\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BestPattern();

    private sealed record RawCandidate(string Phrase, string SourceType, int DisplayOrder);

    private sealed record ScoredCandidate(string Phrase, string SourceType, int DisplayOrder, double Score);
}
