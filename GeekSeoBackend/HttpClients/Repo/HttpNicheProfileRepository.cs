using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpNicheProfileRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : INicheProfileRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles?userId={user.UserId}", profile, ct);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile>(Json, ct);
        return value is null
            ? Result<NicheProfile>.Failure("Empty response")
            : Result<NicheProfile>.Success(value);
    }

    public async Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<NicheProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile?>(Json, ct);
        return Result<NicheProfile?>.Success(value);
    }

    public async Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/project-id?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<Guid?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<Guid?>.Failure(await ReadFailureAsync(res, ct));
        var payload = await res.Content.ReadFromJsonAsync<ProjectIdResponse>(Json, ct);
        return Result<Guid?>.Success(payload?.ProjectId);
    }

    private sealed record ProjectIdResponse(Guid ProjectId);

    public async Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/status-snapshot?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<NicheProfileStatusRow?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfileStatusRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfileStatusRow>(Json, ct);
        return Result<NicheProfileStatusRow?>.Success(value);
    }

    public async Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(
        Guid profileId, bool includeFusion, CancellationToken ct = default)
    {
        var fusion = includeFusion ? "true" : "false";
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/analysis-details-snapshot?includeFusion={fusion}&userId={user.UserId}",
            ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<NicheAnalysisDetailsRow?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheAnalysisDetailsRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<NicheAnalysisDetailsRow>(Json, ct);
        return Result<NicheAnalysisDetailsRow?>.Success(value);
    }

    public async Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/project/{projectId}/latest?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<NicheProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile?>(Json, ct);
        return Result<NicheProfile?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/project/{projectId}/history?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result> UpsertStepRunAsync(
        Guid profileId,
        NicheProfileStepRunUpsert stepRun,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/step-runs/{stepRun.StepSlug}?userId={user.UserId}",
            stepRun,
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdateStepRunStatusAsync(
        Guid profileId,
        string stepSlug,
        NicheProfileStepRunStatusPatch patch,
        CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/step-runs/{stepSlug}/status?userId={user.UserId}",
            patch,
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileStepRunRow>>> GetStepRunsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/step-runs?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileStepRunRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileStepRunRow>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileStepRunRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceSchemaSignalsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileSchemaSignalWrite> signals,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/schema-signals?userId={user.UserId}",
            new { signals },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileSchemaSignalRow>>> GetSchemaSignalsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/schema-signals?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileSchemaSignalRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileSchemaSignalRow>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileSchemaSignalRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceDiscoveredUrlsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileDiscoveredUrlWrite> urls,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/discovered-urls?userId={user.UserId}",
            new { urls },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/discovered-urls?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileDiscoveredUrlRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileDiscoveredUrlRow>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileDiscoveredUrlRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceNavigationLinksAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileNavigationLinkWrite> links,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/navigation-links?userId={user.UserId}",
            new { links },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileNavigationLinkRow>>> GetNavigationLinksAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/navigation-links?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileNavigationLinkRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileNavigationLinkRow>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileNavigationLinkRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceHeadingsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileHeadingWrite> headings,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/headings?userId={user.UserId}",
            new { headings },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileHeadingRow>>> GetHeadingsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/headings?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileHeadingRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileHeadingRow>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileHeadingRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceTopicCandidateEvidenceAsync(
        Guid profileId,
        IReadOnlyList<NicheTopicCandidateEvidenceWrite> evidence,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/topic-candidate-evidence?userId={user.UserId}",
            new { evidence },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<NicheTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/topic-candidate-evidence?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheTopicCandidateEvidenceRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheTopicCandidateEvidenceRow>>(Json, ct);
        return Result<IReadOnlyList<NicheTopicCandidateEvidenceRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplacePageContentAsync(
        Guid profileId,
        NicheProfilePageContentWrite content,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/page-content?userId={user.UserId}",
            new { content },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<NicheProfilePageContentRow?>> GetPageContentAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/page-content?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfilePageContentRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfilePageContentRow>(Json, ct);
        return Result<NicheProfilePageContentRow?>.Success(value);
    }

    public async Task<Result> ReplaceSiteStructureAsync(
        Guid profileId,
        NicheProfileSiteStructureWrite structure,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/site-structure?userId={user.UserId}",
            new { structure },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<NicheProfileSiteStructureRow?>> GetSiteStructureAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/site-structure?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfileSiteStructureRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfileSiteStructureRow>(Json, ct);
        return Result<NicheProfileSiteStructureRow?>.Success(value);
    }

    public async Task<Result> UpdateStatusAsync(
        Guid profileId, string status, string? step = null,
        int stepNumber = 0, int totalSteps = 0, string? errorMessage = null,
        NicheAnalysisStepLogEntry? stepLogEntry = null,
        CancellationToken ct = default)
    {
        var body = new { status, step, stepNumber, totalSteps, errorMessage, stepLogEntry };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/status?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateScoresAsync(
        Guid profileId, decimal authorityScore, int covered, int partial, int gap,
        CancellationToken ct = default)
    {
        var body = new { authorityScore, covered, partial, gap };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/scores?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateProfileSummaryAsync(
        Guid profileId, NicheProfileSummaryPatch summary, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/profile-summary?userId={user.UserId}",
            summary,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> SaveFusionSnapshotAsync(
        Guid profileId, string fusionSnapshotJson, CancellationToken ct = default)
    {
        var body = new { fusionSnapshot = fusionSnapshotJson };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/fusion-snapshot?userId={user.UserId}",
            body,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdatePhaseStatusAsync(
        Guid profileId, NichePhaseStatusPatch patch, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/phase-status?userId={user.UserId}",
            patch,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> BulkUpsertTopicCandidatesAsync(
        Guid profileId,
        IReadOnlyList<NicheTopicCandidateBulkUpsert> candidates,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/seo/internal/niche-profiles/{profileId}/topic-candidates/bulk?userId={user.UserId}");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(candidates, options: Json);
        var res = await _http.SendAsync(request, ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<NicheTopicCandidateListResult>> GetTopicCandidatesAsync(
        Guid profileId,
        int page,
        int pageSize,
        bool? selectedOnly,
        CancellationToken ct = default)
    {
        var selected = selectedOnly switch
        {
            true => "&selectedOnly=true",
            false => "&selectedOnly=false",
            _ => string.Empty,
        };
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/topic-candidates?page={page}&pageSize={pageSize}{selected}&userId={user.UserId}",
            ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<NicheTopicCandidateListResult>.Failure("HTTP 404: topic-candidates route not found");
        if (!res.IsSuccessStatusCode)
            return Result<NicheTopicCandidateListResult>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<NicheTopicCandidateListResult>(Json, ct);
        return value is null
            ? Result<NicheTopicCandidateListResult>.Failure("Empty response")
            : Result<NicheTopicCandidateListResult>.Success(value);
    }

    public async Task<Result> SaveAnalysisResultsAsync(
        Guid profileId, NicheAnalysisSaveRequest results, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/analysis-results?userId={user.UserId}",
            results,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default)
    {
        var body = pillars.Select(NicheBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/pillars?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default)
    {
        var body = subtopics.Select(NicheBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/subtopics?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default)
    {
        var body = competitors.Select(NicheBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/competitors?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateCompetitorInsightsAsync(NicheCompetitor competitor, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/competitors/{competitor.Id}/insights?userId={user.UserId}",
            NicheBulkInsertMapper.ToBulkInsert(competitor), Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default)
    {
        var body = entities.Select(NicheBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/entities?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default)
    {
        var body = pages.Select(NicheBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/pillar-pages?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/maintenance/due?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/maintenance/queued?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheQueuedJob>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheQueuedJob>>(Json, ct);
        return Result<IReadOnlyList<NicheQueuedJob>>.Success(value ?? []);
    }

    public async Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        var minutes = Math.Clamp((int)Math.Ceiling(maxAge.TotalMinutes), 1, 60);
        var res = await _http.PostAsync(
            $"api/seo/internal/niche-profiles/maintenance/fail-stale-processing?maxAgeMinutes={minutes}&userId={user.UserId}",
            null,
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<int>.Failure(await res.Content.ReadAsStringAsync(ct));
        var payload = await res.Content.ReadFromJsonAsync<FailStaleResponse>(Json, ct);
        return Result<int>.Success(payload?.FailedCount ?? 0);
    }

    private sealed record FailStaleResponse(int FailedCount);

    // Step isolation methods
    public async Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status,
        NicheAnalysisStepLogEntry? entry = null, CancellationToken ct = default)
    {
        var payload = new { slug, status, stepLogEntry = entry };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/step-status?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> InvalidateDownstreamStepsAsync(Guid profileId,
        IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default)
    {
        var payload = new { downstreamSlugs };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/invalidate-steps?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson,
        CancellationToken ct = default)
    {
        var payload = new { crawledUrlsJson };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/crawled-urls?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var jsonRes = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}/step-statuses?userId={user.UserId}", ct);
        if (jsonRes.IsSuccessStatusCode)
        {
            var jsonDict = await jsonRes.Content.ReadFromJsonAsync<Dictionary<string, string>>(Json, ct);
            if (jsonDict is not null)
            {
                foreach (var (slug, status) in jsonDict)
                    merged[slug] = NicheStepStatusEnricher.PreferStatus(merged.GetValueOrDefault(slug), status);
            }
        }

        var runs = await GetStepRunsAsync(profileId, ct);
        if (runs.IsSuccess && runs.Value is not null)
        {
            foreach (var run in runs.Value)
                merged[run.StepSlug] = NicheStepStatusEnricher.PreferStatus(
                    merged.GetValueOrDefault(run.StepSlug),
                    run.Status);
        }

        var detailsResult = await GetAnalysisDetailsRowAsync(profileId, includeFusion: false, ct);
        if (detailsResult.IsSuccess && detailsResult.Value is not null)
            NicheStepStatusEnricher.MergeStepLog(merged, detailsResult.Value.AnalysisStepLog);

        if (merged.Count > 0)
            return Result<IReadOnlyDictionary<string, string>>.Success(merged);

        if (!jsonRes.IsSuccessStatusCode)
            return Result<IReadOnlyDictionary<string, string>>.Failure(await ReadFailureAsync(jsonRes, ct));

        return Result<IReadOnlyDictionary<string, string>>.Success(merged);
    }

    private static async Task<string> ReadFailureAsync(HttpResponseMessage res, CancellationToken ct)
    {
        var body = await res.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return $"HTTP {(int)res.StatusCode} {res.ReasonPhrase}";
        return $"HTTP {(int)res.StatusCode}: {body}";
    }
}
