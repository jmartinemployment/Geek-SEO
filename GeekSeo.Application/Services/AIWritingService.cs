using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed partial class AIWritingService(
    IBackgroundJobRepository jobs,
    IProjectRepository projects,
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IAIProvider ai) : IAIWritingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<BackgroundJobStatus>> EnqueueFullArticleAsync(
        Guid userId, FullArticleRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<BackgroundJobStatus>.Failure("Access denied");

        var payload = JsonSerializer.Serialize(new FullArticleJobPayload
        {
            ProjectId = request.ProjectId,
            Keyword = request.Keyword,
            Location = request.Location,
            Title = string.IsNullOrWhiteSpace(request.Title) ? request.Keyword : request.Title,
        }, JsonOptions);

        var created = await jobs.CreateAsync(new CreateBackgroundJobRequest
        {
            UserId = userId,
            ProjectId = request.ProjectId,
            JobType = "full_article",
            PayloadJson = payload,
        }, ct);

        if (!created.IsSuccess || created.Value is null)
            return Result<BackgroundJobStatus>.Failure(created.Error ?? "Failed to create job");

        var job = created.Value;
        return Result<BackgroundJobStatus>.Success(new BackgroundJobStatus
        {
            JobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ProgressPercent = job.ProgressPercent,
        });
    }

    public async Task<Result<BackgroundJobStatus>> EnqueueBulkArticlesAsync(
        Guid userId, BulkArticleRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<BackgroundJobStatus>.Failure("Access denied");

        var keywords = request.Keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (keywords.Count == 0)
            return Result<BackgroundJobStatus>.Failure("At least one keyword is required");

        var location = string.IsNullOrWhiteSpace(request.Location)
            ? project.Value.DefaultLocation
            : request.Location;

        var payload = JsonSerializer.Serialize(new BulkArticleJobPayload
        {
            ProjectId = request.ProjectId,
            Keywords = keywords,
            Location = location,
            CurrentIndex = 0,
        }, JsonOptions);

        var created = await jobs.CreateAsync(new CreateBackgroundJobRequest
        {
            UserId = userId,
            ProjectId = request.ProjectId,
            JobType = "bulk_article",
            PayloadJson = payload,
        }, ct);

        if (!created.IsSuccess || created.Value is null)
            return Result<BackgroundJobStatus>.Failure(created.Error ?? "Failed to create job");

        var job = created.Value;
        return Result<BackgroundJobStatus>.Success(new BackgroundJobStatus
        {
            JobId = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ProgressPercent = job.ProgressPercent,
        });
    }

    public async Task<Result<WritingTextResult>> GenerateOutlineAsync(
        Guid userId, WritingOutlineRequest request, CancellationToken ct = default)
    {
        _ = userId;
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result<WritingTextResult>.Failure("Keyword is required");
        if (request.Brief is null)
            return Result<WritingTextResult>.Failure("Brief is required — generate a brief first");

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ArticlePromptBuilder.BuildOutlineSystemPrompt(request.Brief.Methodology),
            UserPrompt = ArticlePromptBuilder.BuildOutlineUserPrompt(request),
            MaxTokens = 2048,
            Temperature = 0.5,
        }, ct);

        var outlineResult = ToWritingResult(response);
        if (!outlineResult.IsSuccess || outlineResult.Value is null)
            return outlineResult;

        var withMethodology = await ArticleMethodologyOutlineEnricher.EnsureMethodologyOutlineAsync(
            outlineResult.Value.Content,
            request.Brief,
            ai,
            ct);

        return Result<WritingTextResult>.Success(new WritingTextResult
        {
            Content = ArticleClosingFaqEnricher.EnsureClosingFaqOutline(withMethodology, request.Brief),
        });
    }

    public async Task<Result<WritingTextResult>> GenerateDraftAsync(
        Guid userId, WritingDraftRequest request, CancellationToken ct = default)
    {
        _ = userId;
        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ArticlePromptBuilder.BuildDraftSystemPrompt(),
            UserPrompt = ArticlePromptBuilder.BuildDraftUserPrompt(request),
            MaxTokens = 8192,
            Temperature = 0.7,
        }, ct);

        var draftResult = ToWritingResult(response);
        if (!draftResult.IsSuccess || draftResult.Value is null)
            return draftResult;

        var withMethodology = request.Brief is not null
            ? ArticleMethodologyScaffold.SanitizeDraft(
                draftResult.Value.Content,
                request.Brief.Keyword,
                request.Brief.Methodology)
            : ArticleMethodologyScaffold.StripMovementLabels(draftResult.Value.Content);

        var enriched = await ArticleClosingFaqEnricher.EnsureClosingFaqDraftAsync(
            withMethodology,
            request.Brief,
            ai,
            ct);

        return Result<WritingTextResult>.Success(new WritingTextResult
        {
            Content = ArticleMethodologyScaffold.StripMovementLabels(enriched),
        });
    }

    public async Task<Result<WritingTextResult>> GenerateDraftFromResearchAsync(
        Guid userId, ResearchDraftRequest request, CancellationToken ct = default)
    {
        _ = userId;
        if (request.Research is null)
            return Result<WritingTextResult>.Failure("Research context is required");

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ArticlePromptBuilder.BuildResearchDraftSystemPrompt(),
            UserPrompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(request),
            MaxTokens = 8192,
            Temperature = 0.7,
        }, ct);

        var draftResult = ToWritingResult(response);
        if (!draftResult.IsSuccess || draftResult.Value is null)
            return draftResult;

        var withMethodology = await ArticleMethodologyDraftEnricher.EnsureResearchMethodologyDraftAsync(
            draftResult.Value.Content,
            request,
            ai,
            ct);

        var sanitized = ArticleMethodologyScaffold.SanitizeDraft(
            withMethodology,
            request.Research.DerivedKeyword,
            WritingMethodologySpec.FourPhase);

        var enriched = await ArticleClosingFaqEnricher.EnsureClosingFaqDraftAsync(
            sanitized,
            request.Research,
            ai,
            ct);

        return Result<WritingTextResult>.Success(new WritingTextResult
        {
            Content = ArticleMethodologyScaffold.StripMovementLabels(enriched),
        });
    }

    public async Task<Result<WritingTextResult>> HumanizeAsync(
        Guid userId, HumanizeRequest request, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, request.DocumentId, ct);
        if (!access.IsSuccess)
            return Result<WritingTextResult>.Failure(access.Error ?? "Access denied");

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                "Rewrite the HTML to sound more human and conversational while keeping headings and structure. Return HTML only.",
            UserPrompt = request.ContentHtml,
            MaxTokens = 8192,
            Temperature = 0.8,
        }, ct);

        return ToWritingResult(response);
    }

    public async Task<Result<AiDetectionResult>> DetectAsync(
        Guid userId, DetectAiRequest request, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, request.DocumentId, ct);
        if (!access.IsSuccess)
            return Result<AiDetectionResult>.Failure(access.Error ?? "Access denied");

        var plain = StripHtml(request.ContentHtml);
        if (string.IsNullOrWhiteSpace(plain))
            return Result<AiDetectionResult>.Failure("Add article content before running AI detection.");

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                "Estimate how likely text was AI-generated. Reply ONLY with JSON: {\"aiProbability\":0.0,\"summary\":\"one sentence\"}. Use a number between 0 and 1 for aiProbability.",
            UserPrompt = plain.Length > 8000 ? plain[..8000] : plain,
            MaxTokens = 256,
            Temperature = 0,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return Result<AiDetectionResult>.Failure(response.Error ?? "Detection failed");

        if (!AiDetectionResponseParser.TryParse(response.Value.Content, out var prob, out var summary))
            return Result<AiDetectionResult>.Failure("Could not parse AI detection response. Try again.");

        var clamped = Math.Clamp(prob, 0, 1);
        await documentRepo.UpdateAiDetectionScoreAsync(request.DocumentId, (decimal)clamped, ct);
        return Result<AiDetectionResult>.Success(new AiDetectionResult
        {
            AiProbability = clamped,
            Summary = summary,
        });
    }

    private static Result<WritingTextResult> ToWritingResult(Result<AIResponse> response)
    {
        if (!response.IsSuccess || response.Value is null)
            return Result<WritingTextResult>.Failure(response.Error ?? "AI request failed");
        return Result<WritingTextResult>.Success(new WritingTextResult
        {
            Content = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content),
        });
    }

    private static string StripHtml(string html) => HtmlTagRegex().Replace(html, " ");

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
