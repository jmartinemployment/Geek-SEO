using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class LinkedClosingFaqEnricher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<Result<(string Html, int LinkedCount, int PlainTextOnlyCount, IReadOnlyList<string> Skipped)>> EnrichAsync(
        string pillarHtml,
        LinkedFaqEnrichmentRequest request,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (request.FaqAssignments.Count == 0)
            return Result<(string, int, int, IReadOnlyList<string>)>.Failure("FAQ plan is empty.");

        var aiResult = await CompleteWithRetryAsync(request, ai, ct);
        if (!aiResult.IsSuccess || aiResult.Value is null)
            return Result<(string, int, int, IReadOnlyList<string>)>.Failure(aiResult.Error ?? "Linked FAQ generation failed");

        var validation = FaqAnswerValidator.ValidateAll(request.FaqAssignments, aiResult.Value.FaqResults);
        if (validation.AnswersById.Count == 0)
        {
            var fallback = BuildTemplateFallback(request.FaqAssignments);
            validation = FaqAnswerValidator.ValidateAll(request.FaqAssignments, fallback.FaqResults);
        }

        var faqSection = FaqHtmlAssembler.Build(request.FaqAssignments, validation.AnswersById);
        if (string.IsNullOrWhiteSpace(faqSection))
            return Result<(string, int, int, IReadOnlyList<string>)>.Failure("Could not assemble linked FAQ section.");

        var merged = ArticleClosingFaqEnricher.ReplaceClosingFaqSection(pillarHtml, faqSection);
        var linkedCount = request.FaqAssignments.Count(a =>
            a.IsTargetActive && validation.AnswersById.ContainsKey(a.Id) &&
            validation.AnswersById[a.Id].Contains("<a ", StringComparison.OrdinalIgnoreCase));
        var plainTextOnlyCount = validation.AnswersById.Count - linkedCount;

        return Result<(string, int, int, IReadOnlyList<string>)>.Success(
            (merged, linkedCount, plainTextOnlyCount, validation.Skipped));
    }

    private static async Task<Result<LinkedFaqEnrichmentResponse>> CompleteWithRetryAsync(
        LinkedFaqEnrichmentRequest request,
        IAIProvider ai,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await ai.CompleteAsync(new AIRequest
            {
                SystemPrompt = LinkedFaqRequestBuilder.BuildSystemPrompt(),
                UserPrompt = LinkedFaqRequestBuilder.SerializeRequest(request),
                MaxTokens = 4096,
                Temperature = 0.4,
            }, ct);

            if (!response.IsSuccess || response.Value is null)
                continue;

            if (TryParseResponse(response.Value.Content, out var parsed))
                return Result<LinkedFaqEnrichmentResponse>.Success(parsed);
        }

        return Result<LinkedFaqEnrichmentResponse>.Failure("Could not parse linked FAQ JSON from AI response.");
    }

    private static LinkedFaqEnrichmentResponse BuildTemplateFallback(IReadOnlyList<LinkedFaqAssignment> assignments)
    {
        var results = assignments.Select(a => new LinkedFaqResult(
            a.Id,
            a.Question,
            a.IsTargetActive
                ? $"This depends on your stack and goals. See our {a.AnchorText} guide for a practical breakdown."
                : $"This depends on your stack and goals. {a.AnchorText} is a common starting point before you scale.")).ToList();

        return new LinkedFaqEnrichmentResponse(results);
    }

    private static bool TryParseResponse(string raw, out LinkedFaqEnrichmentResponse response)
    {
        response = new LinkedFaqEnrichmentResponse([]);
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed[start..(end + 1)]);
            if (!doc.RootElement.TryGetProperty("faqResults", out var faqResults) ||
                faqResults.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var list = new List<LinkedFaqResult>();
            foreach (var item in faqResults.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString()?.Trim() : null;
                var question = item.TryGetProperty("question", out var qEl) ? qEl.GetString()?.Trim() : null;
                var answerHtml = item.TryGetProperty("answerHtml", out var aEl) ? aEl.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answerHtml))
                    continue;

                list.Add(new LinkedFaqResult(id, question, answerHtml));
            }

            if (list.Count == 0)
                return false;

            response = new LinkedFaqEnrichmentResponse(list);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
