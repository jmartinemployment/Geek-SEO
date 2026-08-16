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
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSiteAnalysisProfileRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : ISiteAnalysisProfileRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<Result<SiteAnalysisProfile>> CreateAsync(SiteAnalysisProfile profile, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles?userId={user.UserId}", profile, ct);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfile>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfile>(Json, ct);
        return value is null
            ? Result<SiteAnalysisProfile>.Failure("Empty response")
            : Result<SiteAnalysisProfile>.Success(value);
    }

    public async Task<Result<SiteAnalysisProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<SiteAnalysisProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfile?>(Json, ct);
        return Result<SiteAnalysisProfile?>.Success(value);
    }

    public async Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/project-id?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<Guid?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<Guid?>.Failure(await ReadFailureAsync(res, ct));
        var payload = await res.Content.ReadFromJsonAsync<ProjectIdResponse>(Json, ct);
        return Result<Guid?>.Success(payload?.ProjectId);
    }

    private sealed record ProjectIdResponse(Guid ProjectId);

    public async Task<Result<SiteAnalysisProfileStatusRow?>> GetStatusRowAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/status-snapshot?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<SiteAnalysisProfileStatusRow?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfileStatusRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfileStatusRow>(Json, ct);
        return Result<SiteAnalysisProfileStatusRow?>.Success(value);
    }

    public async Task<Result<SiteAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(
        Guid profileId, bool includeFusion, CancellationToken ct = default)
    {
        var fusion = includeFusion ? "true" : "false";
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/analysis-details-snapshot?includeFusion={fusion}&userId={user.UserId}",
            ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<SiteAnalysisDetailsRow?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisDetailsRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisDetailsRow>(Json, ct);
        return Result<SiteAnalysisDetailsRow?>.Success(value);
    }

    public async Task<Result<SiteAnalysisProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/project/{projectId}/latest?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<SiteAnalysisProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfile?>(Json, ct);
        return Result<SiteAnalysisProfile?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> GetHistoryAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/project/{projectId}/history?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListRecentAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/recent?userId={user.UserId}&limit={Math.Clamp(limit, 1, 200)}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListByNormalizedDomainAsync(
        string normalizedHost, int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/by-domain?userId={user.UserId}&domain={Uri.EscapeDataString(normalizedHost)}&limit={Math.Clamp(limit, 1, 200)}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>> FindTreesByKeywordAsync(
        Guid siteAnalysisProfileId, string keyword, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{siteAnalysisProfileId}/trees-by-keyword?userId={user.UserId}&keyword={Uri.EscapeDataString(keyword)}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisPageSectionTreeRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>.Success(value ?? []);
    }

    public async Task<Result> UpsertStepRunAsync(
        Guid profileId,
        SiteAnalysisProfileStepRunUpsert stepRun,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/step-runs/{stepRun.StepSlug}?userId={user.UserId}",
            stepRun,
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdateStepRunStatusAsync(
        Guid profileId,
        string stepSlug,
        SiteAnalysisProfileStepRunStatusPatch patch,
        CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/step-runs/{stepSlug}/status?userId={user.UserId}",
            patch,
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>> GetStepRunsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/step-runs?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileStepRunRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceSchemaSignalsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileSchemaSignalWrite> signals,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/schema-signals?userId={user.UserId}",
            new { signals },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>> GetSchemaSignalsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/schema-signals?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileSchemaSignalRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceDiscoveredUrlsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileDiscoveredUrlWrite> urls,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/discovered-urls?userId={user.UserId}",
            new { urls },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/discovered-urls?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileDiscoveredUrlRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceNavigationLinksAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileNavigationLinkWrite> links,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/navigation-links?userId={user.UserId}",
            new { links },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>> GetNavigationLinksAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/navigation-links?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileNavigationLinkRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceHeadingsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileHeadingWrite> headings,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/headings?userId={user.UserId}",
            new { headings },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>> GetHeadingsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/headings?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileHeadingRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplacePageSectionTreesAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisPageSectionTreeWrite> pages,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/page-section-trees?userId={user.UserId}",
            new { pages },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>> GetPageSectionTreesAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/page-section-trees?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisPageSectionTreeRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceExtractedToolsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileExtractedToolWrite> tools,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/extracted-tools?userId={user.UserId}",
            new { tools },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>> GetExtractedToolsAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/extracted-tools?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileExtractedToolRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplaceTopicCandidateEvidenceAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisTopicCandidateEvidenceWrite> evidence,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/topic-candidate-evidence?userId={user.UserId}",
            new { evidence },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/topic-candidate-evidence?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisTopicCandidateEvidenceRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>.Success(value ?? []);
    }

    public async Task<Result> ReplacePageContentAsync(
        Guid profileId,
        SiteAnalysisProfilePageContentWrite content,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/page-content?userId={user.UserId}",
            new { content },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<SiteAnalysisProfilePageContentRow?>> GetPageContentAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/page-content?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfilePageContentRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfilePageContentRow>(Json, ct);
        return Result<SiteAnalysisProfilePageContentRow?>.Success(value);
    }

    public async Task<Result> ReplaceSiteStructureAsync(
        Guid profileId,
        SiteAnalysisProfileSiteStructureWrite structure,
        CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/site-structure?userId={user.UserId}",
            new { structure },
            Json,
            ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<SiteAnalysisProfileSiteStructureRow?>> GetSiteStructureAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/site-structure?userId={user.UserId}",
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfileSiteStructureRow?>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfileSiteStructureRow>(Json, ct);
        return Result<SiteAnalysisProfileSiteStructureRow?>.Success(value);
    }

    public async Task<Result> UpdateStatusAsync(
        Guid profileId, string status, string? step = null,
        int stepNumber = 0, int totalSteps = 0, string? errorMessage = null,
        SiteAnalysisStepLogEntry? stepLogEntry = null,
        CancellationToken ct = default)
    {
        var body = new { status, step, stepNumber, totalSteps, errorMessage, stepLogEntry };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/status?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateScoresAsync(
        Guid profileId, decimal authorityScore, int covered, int partial, int gap,
        CancellationToken ct = default)
    {
        var body = new { authorityScore, covered, partial, gap };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/scores?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateProfileSummaryAsync(
        Guid profileId, SiteAnalysisProfileSummaryPatch summary, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/profile-summary?userId={user.UserId}",
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
            $"api/seo/internal/site-analysis-profiles/{profileId}/fusion-snapshot?userId={user.UserId}",
            body,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdatePhaseStatusAsync(
        Guid profileId, SiteAnalysisPhaseStatusPatch patch, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/phase-status?userId={user.UserId}",
            patch,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> BulkUpsertTopicCandidatesAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisTopicCandidateBulkUpsert> candidates,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/seo/internal/site-analysis-profiles/{profileId}/topic-candidates/bulk?userId={user.UserId}");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(candidates, options: Json);
        var res = await _http.SendAsync(request, ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<SiteAnalysisTopicCandidateListResult>> GetTopicCandidatesAsync(
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
            $"api/seo/internal/site-analysis-profiles/{profileId}/topic-candidates?page={page}&pageSize={pageSize}{selected}&userId={user.UserId}",
            ct);
        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<SiteAnalysisTopicCandidateListResult>.Failure("HTTP 404: topic-candidates route not found");
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisTopicCandidateListResult>.Failure(await ReadFailureAsync(res, ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisTopicCandidateListResult>(Json, ct);
        return value is null
            ? Result<SiteAnalysisTopicCandidateListResult>.Failure("Empty response")
            : Result<SiteAnalysisTopicCandidateListResult>.Success(value);
    }

    public async Task<Result> SaveAnalysisResultsAsync(
        Guid profileId, SiteAnalysisSaveRequest results, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/analysis-results?userId={user.UserId}",
            results,
            Json,
            ct);
        return res.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> BulkInsertPillarsAsync(IEnumerable<SiteAnalysisPillar> pillars, CancellationToken ct = default)
    {
        var body = pillars.Select(SiteAnalysisBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/pillars?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertSubtopicsAsync(IEnumerable<SiteAnalysisSubtopic> subtopics, CancellationToken ct = default)
    {
        var body = subtopics.Select(SiteAnalysisBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/subtopics?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertCompetitorsAsync(IEnumerable<SiteAnalysisCompetitor> competitors, CancellationToken ct = default)
    {
        var body = competitors.Select(SiteAnalysisBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/competitors?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisCompetitor>>> GetCompetitorsAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/competitors?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<IReadOnlyList<SiteAnalysisCompetitor>>.Success([]);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisCompetitor>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisCompetitor>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisCompetitor>>.Success(value ?? []);
    }

    public async Task<Result> UpdateCompetitorInsightsAsync(SiteAnalysisCompetitor competitor, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/competitors/{competitor.Id}/insights?userId={user.UserId}",
            SiteAnalysisBulkInsertMapper.ToBulkInsert(competitor), Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertEntitiesAsync(IEnumerable<SiteAnalysisEntity> entities, CancellationToken ct = default)
    {
        var body = entities.Select(SiteAnalysisBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/entities?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertPillarPagesAsync(IEnumerable<SiteAnalysisPillarPage> pages, CancellationToken ct = default)
    {
        var body = pages.Select(SiteAnalysisBulkInsertMapper.ToBulkInsert).ToList();
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/pillar-pages?userId={user.UserId}", body, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListDueForReanalysisAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/maintenance/due?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<SiteAnalysisQueuedJob>>> ListQueuedAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/maintenance/queued?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalysisQueuedJob>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<SiteAnalysisQueuedJob>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalysisQueuedJob>>.Success(value ?? []);
    }

    public async Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        var minutes = Math.Clamp((int)Math.Ceiling(maxAge.TotalMinutes), 1, 60);
        var res = await _http.PostAsync(
            $"api/seo/internal/site-analysis-profiles/maintenance/fail-stale-processing?maxAgeMinutes={minutes}&userId={user.UserId}",
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
        SiteAnalysisStepLogEntry? entry = null, CancellationToken ct = default)
    {
        var payload = new { slug, status, stepLogEntry = entry };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/step-status?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> InvalidateDownstreamStepsAsync(Guid profileId,
        IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default)
    {
        var payload = new { downstreamSlugs };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/invalidate-steps?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson,
        CancellationToken ct = default)
    {
        var payload = new { crawledUrlsJson };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/crawled-urls?userId={user.UserId}",
            payload, Json, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ReadFailureAsync(res, ct));
    }

    public async Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var jsonRes = await _http.GetAsync(
            $"api/seo/internal/site-analysis-profiles/{profileId}/step-statuses?userId={user.UserId}", ct);
        if (jsonRes.IsSuccessStatusCode)
        {
            var jsonDict = await jsonRes.Content.ReadFromJsonAsync<Dictionary<string, string>>(Json, ct);
            if (jsonDict is not null)
            {
                foreach (var (slug, status) in jsonDict)
                    merged[slug] = SiteAnalyzerStepStatusEnricher.PreferStatus(merged.GetValueOrDefault(slug), status);
            }
        }

        var runs = await GetStepRunsAsync(profileId, ct);
        if (runs.IsSuccess && runs.Value is not null)
        {
            foreach (var run in runs.Value)
                merged[run.StepSlug] = SiteAnalyzerStepStatusEnricher.PreferStatus(
                    merged.GetValueOrDefault(run.StepSlug),
                    run.Status);
        }

        var detailsResult = await GetAnalysisDetailsRowAsync(profileId, includeFusion: false, ct);
        if (detailsResult.IsSuccess && detailsResult.Value is not null)
            SiteAnalyzerStepStatusEnricher.MergeStepLog(merged, detailsResult.Value.AnalysisStepLog);

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
